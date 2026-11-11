using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class SynchronizationCoordinator(
    CodeFirstSynchronizer synchronizer,
    ICodeFirstDefinitionRegistry registry,
    ISynchronizationReportStore reportStore,
    ICoreScopeProvider scopes,
    IKeyValueService values,
    IDistributedLockingMechanismFactory lockingFactory,
    TimeProvider timeProvider,
    ILogger<SynchronizationCoordinator> logger) : ICodeFirstSynchronizer, IDisposable {
    internal const string CoordinationKey = "shorthand.umbraco-codefirst:synchronization-coordination";
    private readonly string _instance = Guid.NewGuid().ToString("N");
    private readonly SemaphoreSlim _localLock = new(1, 1);
    private bool _registered;
    private bool _disposed;

    public async Task<SynchronizationReport> SynchronizeAsync(CancellationToken cancellationToken = default) {
        await _localLock.WaitAsync(cancellationToken);
        try {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var fingerprint = DefinitionFingerprint.Create(registry.Definitions);
            var timeout = Stopwatch.StartNew();
            while(!TryAcquire(fingerprint)) {
                if(timeout.Elapsed >= TimeSpan.FromMinutes(2)) {
                    throw new TimeoutException("Timed out waiting for another code-first synchronization. A stopped instance may require recovery.");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            try {
                return await synchronizer.SynchronizeAsync(cancellationToken);
            } finally {
                ReleaseRun();
            }
        } catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested) {
            throw;
        } catch(Exception exception) {
            logger.LogError(exception, "Code-first synchronization could not run");
            var report = new SynchronizationReport(timeProvider.GetUtcNow(), registry.Definitions
                .Select(definition => new SynchronizationItem(
                    definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed, exception.Message))
                .ToArray()) { Error = exception.Message };
            reportStore.Set(report);
            return report;
        } finally {
            _localLock.Release();
        }
    }

    public void Dispose() {
        _localLock.Wait();
        try {
            if(_disposed) {
                return;
            }

            _disposed = true;
            if(_registered) {
                try {
                    UpdateState(state => {
                        state.Members.Remove(_instance);
                        return true;
                    });
                } catch(Exception exception) {
                    logger.LogError(exception, "Could not release code-first instance membership; recovery may be required");
                }
            }
        } finally {
            _localLock.Release();
        }
    }

    private bool TryAcquire(string fingerprint) {
        var acquired = UpdateState(state => {
            if(state.Members.Any(member => member.Key != _instance && member.Value != fingerprint)) {
                throw new InvalidOperationException(
                    "Code-first synchronization is blocked because another instance has different definitions. Drain old instances before deploying new definitions.");
            }

            if(state.Owner is not null) {
                return false;
            }

            state.Members[_instance] = fingerprint;
            state.Owner = _instance;
            return true;
        });
        if(acquired) {
            _registered = true;
        }

        return acquired;
    }

    private void ReleaseRun() => UpdateState(state => {
        if(state.Owner != _instance) {
            throw new InvalidOperationException("The code-first synchronization claim was lost; recovery is required.");
        }

        state.Owner = null;
        return true;
    });

    private bool UpdateState(Func<CoordinationState, bool> update) {
        if(!lockingFactory.DistributedLockingMechanism.Enabled) {
            throw new InvalidOperationException("Code-first synchronization requires an enabled distributed locking mechanism.");
        }

        if(scopes.Context is not null) {
            throw new InvalidOperationException("Code-first synchronization cannot run inside an existing Umbraco scope.");
        }

        using var scope = scopes.CreateCoreScope();
        scope.EagerWriteLock(UmbracoConstants.Locks.KeyValues);
        var json = values.GetValue(CoordinationKey);
        var state = ReadState(json);

        var changed = update(state);
        if(changed) {
            values.SetValue(CoordinationKey, JsonSerializer.Serialize(state));
        }

        scope.Complete();
        return changed;
    }

    private static CoordinationState ReadState(string? json) {
        if(string.IsNullOrEmpty(json)) {
            return new CoordinationState();
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if(root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(nameof(CoordinationState.Version), out var version)
            || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var value) || value != 1
            || !root.TryGetProperty(nameof(CoordinationState.Members), out var members)
            || members.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(nameof(CoordinationState.Owner), out var owner)
            || owner.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)
            || members.EnumerateObject().Any(member => string.IsNullOrWhiteSpace(member.Name)
                || member.Value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(member.Value.GetString()))) {
            throw new InvalidOperationException("The code-first synchronization coordination state is invalid or unsupported.");
        }

        var state = JsonSerializer.Deserialize<CoordinationState>(json)
            ?? throw new InvalidOperationException("The code-first synchronization coordination state is invalid.");
        if(state.Owner is not null && !state.Members.ContainsKey(state.Owner)) {
            throw new InvalidOperationException("The code-first synchronization coordination owner is invalid.");
        }

        return state;
    }

    private sealed class CoordinationState {
        public int Version { get; set; } = 1;
        public Dictionary<string, string> Members { get; set; } = [];
        public string? Owner { get; set; }
    }
}

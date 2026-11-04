using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Reconciliation;

public sealed record CodeFirstReconciliationPreview(
    SynchronizationReport Synchronization,
    CodeFirstCleanupResponse Cleanup);

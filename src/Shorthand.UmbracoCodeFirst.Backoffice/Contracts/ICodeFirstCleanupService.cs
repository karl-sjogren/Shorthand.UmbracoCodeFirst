using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Contracts;

public interface ICodeFirstCleanupService {
    Task<CodeFirstCleanupResponse> PreviewAsync();

    Task<CodeFirstCleanupResult> ApplyAsync();
}

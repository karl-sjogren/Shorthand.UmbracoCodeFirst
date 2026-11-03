using Microsoft.OpenApi;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Composers;

public class ShorthandUmbracoCodeFirstBackofficeApiComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddBackOfficeOpenApiDocument(
            Constants.ApiName,
            document => document
                .WithTitle("Shorthand Umbraco Code First Backoffice Backoffice API")
                .WithBackOfficeAuthentication()
                .WithJsonOptions(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)
                .ConfigureOpenApiOptions(options =>
                    options.AddDocumentTransformer((doc, _, _) => {
                        doc.Info.Version = "1.0";
                        doc.Info.Contact = new OpenApiContact {
                            Name = "Karl-Johan Sjögren",
                            Email = "",
                            Url = new Uri("https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst")
                        };
                        return Task.CompletedTask;
                    })));
}

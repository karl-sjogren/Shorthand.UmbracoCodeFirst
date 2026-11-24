using Shorthand.UmbracoCodeFirst.Backoffice.Composers;

var builder = WebApplication.CreateBuilder(args);
var umbraco = builder.CreateUmbracoBuilder().AddBackOffice().AddWebsite();
new ShorthandUmbracoCodeFirstBackofficeApiComposer().Compose(umbraco);
umbraco.Build();

_ = new Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry();

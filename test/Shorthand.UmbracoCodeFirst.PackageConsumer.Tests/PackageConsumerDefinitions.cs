using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.PackageConsumer.Tests;

[UCFDataType("Package consumer text", typeof(UmbracoEditors.TextBox), typeof(PackageConsumerTextConfiguration))]
public partial class PackageConsumerTextDataType;

public sealed class PackageConsumerTextConfiguration : UmbracoEditors.TextBox.Configuration;

[UCFDocument("packageConsumer", "Package consumer page",
    DefaultTemplateAlias = UmbracoTemplates.PackageConsumer,
    AllowedTemplateAliases = new[] { UmbracoTemplates.PackageConsumer })]
public partial class PackageConsumerPage;

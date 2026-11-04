namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFValidationAttribute(string expression, string? message = null) : Attribute {
    public string Expression { get; } = expression;

    public string? Message { get; } = message;
}

namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Sets regular-expression validation for a code-first property.</summary>
/// <param name="expression">The regular expression used to validate the property value.</param>
/// <param name="message">The optional message shown when validation fails.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFValidationAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string expression, string? message = null) : Attribute {
    /// <summary>Gets the regular expression used for validation.</summary>
    public string Expression { get; } = expression;

    /// <summary>Gets the optional validation failure message.</summary>
    public string? Message { get; } = message;
}

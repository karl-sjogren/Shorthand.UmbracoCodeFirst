namespace Shorthand.UmbracoCodeFirst.Attributes;

public abstract class UCFTypeAttribute : Attribute {
    protected UCFTypeAttribute(string name) {
        Name = name;
    }

    protected UCFTypeAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    public string? Alias { get; }

    public string Name { get; }

    public string? Icon { get; set; }

    public string? Variations { get; set; }
}

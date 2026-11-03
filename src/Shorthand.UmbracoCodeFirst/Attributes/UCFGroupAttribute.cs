namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFGroupAttribute : Attribute {
    public UCFGroupAttribute(string name) {
        Name = name;
    }

    public UCFGroupAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    public UCFGroupAttribute(string alias, string name, int sortOrder) {
        Alias = alias;
        Name = name;
        SortOrder = sortOrder;
    }

    public string? Alias { get; }

    public string Name { get; }

    public int SortOrder { get; set; }
}

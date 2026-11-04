namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFTabAttribute : Attribute {
    public UCFTabAttribute(string name) {
        Name = name;
    }

    public UCFTabAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    public UCFTabAttribute(string name, int sortOrder) {
        Name = name;
        SortOrder = sortOrder;
    }

    public UCFTabAttribute(string alias, string name, int sortOrder) {
        Alias = alias;
        Name = name;
        SortOrder = sortOrder;
    }

    public string? Alias { get; }

    public string Name { get; }

    public int SortOrder { get; set; }
}

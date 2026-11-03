namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class UCFPropertyAttribute : Attribute {
    public UCFPropertyAttribute(string name, string dataTypeAlias) {
        Name = name;
        DataTypeAlias = dataTypeAlias;
    }

    public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) {
        Alias = alias;
        Name = name;
        DataTypeAlias = dataTypeAlias;
    }

    public string? Alias { get; }

    public string Name { get; }

    public string DataTypeAlias { get; }

    public int SortOrder { get; set; }

    public bool Mandatory { get; set; }

    public bool LabelOnTop { get; set; }
}

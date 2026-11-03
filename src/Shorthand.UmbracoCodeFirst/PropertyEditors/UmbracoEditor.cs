namespace Shorthand.UmbracoCodeFirst.PropertyEditors;

public sealed record UmbracoEditor(string Alias, string? UiAlias = null);

public static class UmbracoEditors {
    public static class TextBox {
        public const string Alias = "Umbraco.TextBox";
        public const string UiAlias = "Umb.PropertyEditorUi.TextBox";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class TextArea {
        public const string Alias = "Umbraco.TextArea";
        public const string UiAlias = "Umb.PropertyEditorUi.TextArea";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class Integer {
        public const string Alias = "Umbraco.Integer";
        public const string UiAlias = "Umb.PropertyEditorUi.Integer";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class Decimal {
        public const string Alias = "Umbraco.Decimal";
        public const string UiAlias = "Umb.PropertyEditorUi.Decimal";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class DateTime {
        public const string Alias = "Umbraco.DateTime";
        public const string UiAlias = "Umb.PropertyEditorUi.DatePicker";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class Boolean {
        public const string Alias = "Umbraco.TrueFalse";
        public const string UiAlias = "Umb.PropertyEditorUi.Toggle";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }

    public static class EmailAddress {
        public const string Alias = "Umbraco.EmailAddress";
        public const string UiAlias = "Umb.PropertyEditorUi.EmailAddress";

        public static UmbracoEditor Definition { get; } = new(Alias, UiAlias);
    }
}

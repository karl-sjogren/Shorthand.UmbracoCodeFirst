using System.Linq.Expressions;
using System.Reflection;
using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public static class CodeFirstHelper<T> {
    public static string Alias<TProperty>(Expression<Func<T, TProperty>> selector) {
        ArgumentNullException.ThrowIfNull(selector);

        Expression body = selector.Body;
        while(body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            && (conversion.Type.IsAssignableFrom(conversion.Operand.Type)
                || Nullable.GetUnderlyingType(conversion.Type) == conversion.Operand.Type)) {
            body = conversion.Operand;
        }

        if(body is not MemberExpression { Member: PropertyInfo property } member
            || member.Expression != selector.Parameters[0]
            || property.GetIndexParameters().Length != 0
            || property.GetCustomAttribute<UCFPropertyAttribute>(inherit: false) is null) {
            throw new ArgumentException("Select a direct property marked with UCFProperty.", nameof(selector));
        }

        var field = property.DeclaringType?
            .GetNestedType("PropertyAliases", BindingFlags.Public)
            ?.GetField(property.Name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if(field is not { IsLiteral: true, FieldType: var fieldType }
            || fieldType != typeof(string)
            || field.GetRawConstantValue() is not string alias
            || string.IsNullOrWhiteSpace(alias)) {
            throw new InvalidOperationException(
                $"Property '{property.DeclaringType?.FullName}.{property.Name}' has no generated code-first property alias.");
        }

        return alias;
    }
}

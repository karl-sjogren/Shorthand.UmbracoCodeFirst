using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ProductDetailsTabAttribute()
    : UCFTabAttribute("Product details", 0);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PricingGroupAttribute()
    : UCFGroupAttribute("pricing", "Pricing", 1);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PricingTabAttribute()
    : UCFTabAttribute("Pricing", 1);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class InventoryTabAttribute()
    : UCFTabAttribute("Inventory", 2);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarketingTabAttribute()
    : UCFTabAttribute("Marketing", 3);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SeoTabAttribute()
    : UCFTabAttribute("SEO", 4);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SupportTabAttribute()
    : UCFTabAttribute("Support", 5);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class LifecycleTabAttribute()
    : UCFTabAttribute("Lifecycle", 6);

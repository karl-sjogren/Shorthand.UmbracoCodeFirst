using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ProductDetailsTabAttribute()
    : UCFTabAttribute("productDetails", "Product details", 0);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PricingGroupAttribute()
    : UCFGroupAttribute("pricing", "Pricing", 1);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PricingTabAttribute()
    : UCFTabAttribute("pricing", "Pricing", 1);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class InventoryTabAttribute()
    : UCFTabAttribute("inventory", "Inventory", 2);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarketingTabAttribute()
    : UCFTabAttribute("marketing", "Marketing", 3);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SeoTabAttribute()
    : UCFTabAttribute("seo", "SEO", 4);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SupportTabAttribute()
    : UCFTabAttribute("support", "Support", 5);

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class LifecycleTabAttribute()
    : UCFTabAttribute("lifecycle", "Lifecycle", 6);

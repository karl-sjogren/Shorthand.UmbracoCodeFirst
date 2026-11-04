using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "Product Page",
    Icon = "icon-barcode",
    AllowAsRoot = true,
    IsListView = true,
    Variations = "Invariant")]
[UCFComposite(typeof(HappySparkles))]
[UCFComposite(typeof(SeoComposite))]
[UCFDescription("A deliberately comprehensive product page used to exercise the code-first model generator.")]
public partial class ProductPage {
    [UCFProperty("Product name", "codeFirstText", SortOrder = 0, Mandatory = true, LabelOnTop = true)]
    [UCFDescription("The customer-facing product name.")]
    [ProductDetailsTab]
    [UCFValidation(@"^.{3,120}$", "The product name must contain between 3 and 120 characters.")]
    public partial string ProductName { get; }

    [UCFProperty("Short description", "codeFirstText", SortOrder = 1, Mandatory = true)]
    [ProductDetailsTab]
    [UCFValidation(@"^.{10,255}$", "Enter a short description between 10 and 255 characters.")]
    public partial string ShortDescription { get; }

    [UCFProperty("Description", CodeFirstLongTextDataType.Alias, SortOrder = 2)]
    [ProductDetailsTab]
    public partial string Description { get; }

    [UCFProperty("SKU", "codeFirstText", SortOrder = 3, Mandatory = true)]
    [ProductDetailsTab]
    [UCFValidation(@"^[A-Z0-9-]{4,32}$", "Use 4-32 uppercase letters, numbers, or hyphens.")]
    public partial string Sku { get; }

    [UCFProperty("Brand", "codeFirstText", SortOrder = 4)]
    [ProductDetailsTab]
    public partial string Brand { get; }

    [UCFProperty("Manufacturer", "codeFirstText", SortOrder = 5)]
    [ProductDetailsTab]
    public partial string Manufacturer { get; }

    [UCFProperty("Model number", "codeFirstText", SortOrder = 6)]
    [ProductDetailsTab]
    public partial string ModelNumber { get; }

    [UCFProperty("Condition", "codeFirstText", SortOrder = 7)]
    [ProductDetailsTab]
    public partial string Condition { get; }

    [UCFProperty("Product code", "codeFirstText", SortOrder = 8)]
    [ProductDetailsTab]
    public partial string ProductCode { get; }

    [UCFProperty("Barcode", "codeFirstText", SortOrder = 9)]
    [ProductDetailsTab]
    [UCFValidation(@"^[0-9]{8,14}$", "Enter an 8-14 digit barcode.")]
    public partial string Barcode { get; }

    [UCFProperty("Price", "codeFirstDecimal", SortOrder = 10, Mandatory = true)]
    [PricingTab]
    [PricingGroup]
    [UCFValidation(@"^[0-9]+([.,][0-9]{1,2})?$", "Enter a non-negative price with up to two decimals.")]
    public partial decimal Price { get; }

    [UCFProperty("Sale price", "codeFirstDecimal", SortOrder = 11)]
    [PricingTab]
    [PricingGroup]
    public partial decimal SalePrice { get; }

    [UCFProperty("Cost price", "codeFirstDecimal", SortOrder = 12)]
    [PricingTab]
    [PricingGroup]
    public partial decimal CostPrice { get; }

    [UCFProperty("Tax rate", "codeFirstDecimal", SortOrder = 13)]
    [PricingTab]
    [PricingGroup]
    public partial decimal TaxRate { get; }

    [UCFProperty("Currency", "codeFirstText", SortOrder = 14)]
    [PricingTab]
    [PricingGroup]
    [UCFValidation(@"^[A-Z]{3}$", "Use a three-letter ISO currency code.")]
    public partial string Currency { get; }

    [UCFProperty("Discount percent", "codeFirstDecimal", SortOrder = 15)]
    [PricingTab]
    [PricingGroup]
    public partial decimal DiscountPercent { get; }

    [UCFProperty("Price label", "codeFirstText", SortOrder = 16)]
    [PricingTab]
    [PricingGroup]
    public partial string PriceLabel { get; }

    [UCFProperty("VAT included", "codeFirstBoolean", SortOrder = 17)]
    [PricingTab]
    [PricingGroup]
    public partial bool VatIncluded { get; }

    [UCFProperty("Subscription product", "codeFirstBoolean", SortOrder = 18)]
    [PricingTab]
    [PricingGroup]
    public partial bool Subscription { get; }

    [UCFProperty("Price note", "codeFirstText", SortOrder = 19)]
    [PricingTab]
    [PricingGroup]
    public partial string PriceNote { get; }

    [UCFProperty("Stock quantity", "codeFirstInteger", SortOrder = 20, Mandatory = true)]
    [InventoryTab]
    public partial int StockQuantity { get; }

    [UCFProperty("Low stock threshold", "codeFirstInteger", SortOrder = 21)]
    [InventoryTab]
    public partial int LowStockThreshold { get; }

    [UCFProperty("Backorders allowed", "codeFirstBoolean", SortOrder = 22)]
    [InventoryTab]
    public partial bool BackorderAllowed { get; }

    [UCFProperty("Warehouse code", "codeFirstText", SortOrder = 23)]
    [InventoryTab]
    public partial string WarehouseCode { get; }

    [UCFProperty("Aisle", "codeFirstText", SortOrder = 24)]
    [InventoryTab]
    public partial string Aisle { get; }

    [UCFProperty("Bin", "codeFirstText", SortOrder = 25)]
    [InventoryTab]
    public partial string Bin { get; }

    [UCFProperty("Restock date", "codeFirstDate", SortOrder = 26)]
    [InventoryTab]
    public partial DateTime RestockDate { get; }

    [UCFProperty("Weight in grams", "codeFirstInteger", SortOrder = 27)]
    [InventoryTab]
    public partial int WeightGrams { get; }

    [UCFProperty("Length in millimeters", "codeFirstInteger", SortOrder = 28)]
    [InventoryTab]
    public partial int LengthMillimeters { get; }

    [UCFProperty("Width in millimeters", "codeFirstInteger", SortOrder = 29)]
    [InventoryTab]
    public partial int WidthMillimeters { get; }

    [UCFProperty("Height in millimeters", "codeFirstInteger", SortOrder = 30)]
    [InventoryTab]
    public partial int HeightMillimeters { get; }

    [UCFProperty("Hero title", "codeFirstText", SortOrder = 31)]
    [MarketingTab]
    public partial string HeroTitle { get; }

    [UCFProperty("Hero subtitle", "codeFirstText", SortOrder = 32)]
    [MarketingTab]
    public partial string HeroSubtitle { get; }

    [UCFProperty("Call to action label", "codeFirstText", SortOrder = 33)]
    [MarketingTab]
    public partial string CallToActionLabel { get; }

    [UCFProperty("Call to action URL", "codeFirstText", SortOrder = 34)]
    [MarketingTab]
    [UCFValidation(@"^https?://", "Enter an absolute HTTP or HTTPS URL.")]
    public partial string CallToActionUrl { get; }

    [UCFProperty("Marketing badge", "codeFirstText", SortOrder = 35)]
    [MarketingTab]
    public partial string MarketingBadge { get; }

    [UCFProperty("SEO title", "codeFirstText", SortOrder = 36)]
    [SeoTab]
    [UCFValidation(@"^.{0,60}$", "The SEO title must be at most 60 characters.")]
    public partial string SeoTitle { get; }

    [UCFProperty("SEO description", "codeFirstLongText", SortOrder = 37)]
    [SeoTab]
    [UCFValidation(@"^.{0,160}$", "The SEO description must be at most 160 characters.")]
    public partial string SeoDescription { get; }

    [UCFProperty("SEO keywords", "codeFirstText", SortOrder = 38)]
    [SeoTab]
    public partial string SeoKeywords { get; }

    [UCFProperty("Canonical URL", "codeFirstText", SortOrder = 39)]
    [SeoTab]
    public partial string CanonicalUrl { get; }

    [UCFProperty("No index", "codeFirstBoolean", SortOrder = 40)]
    [SeoTab]
    public partial bool NoIndex { get; }

    [UCFProperty("Open Graph title", "codeFirstText", SortOrder = 41)]
    [SeoTab]
    public partial string OpenGraphTitle { get; }

    [UCFProperty("Open Graph description", "codeFirstLongText", SortOrder = 42)]
    [SeoTab]
    public partial string OpenGraphDescription { get; }

    [UCFProperty("Twitter title", "codeFirstText", SortOrder = 43)]
    [SeoTab]
    public partial string TwitterTitle { get; }

    [UCFProperty("Twitter description", "codeFirstLongText", SortOrder = 44)]
    [SeoTab]
    public partial string TwitterDescription { get; }

    [UCFProperty("Search boost", "codeFirstDecimal", SortOrder = 45)]
    [SeoTab]
    public partial decimal SearchBoost { get; }

    [UCFProperty("Support email", "codeFirstEmail", SortOrder = 46)]
    [SupportTab]
    [UCFValidation(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Enter a valid support email address.")]
    public partial string SupportEmail { get; }

    [UCFProperty("Support phone", "codeFirstText", SortOrder = 47)]
    [SupportTab]
    [UCFValidation(@"^[0-9+ ()-]{7,24}$", "Enter a valid support phone number.")]
    public partial string SupportPhone { get; }

    [UCFProperty("Support hours", "codeFirstText", SortOrder = 48)]
    [SupportTab]
    public partial string SupportHours { get; }

    [UCFProperty("Support URL", "codeFirstText", SortOrder = 49)]
    [SupportTab]
    public partial string SupportUrl { get; }

    [UCFProperty("Warranty months", "codeFirstInteger", SortOrder = 50)]
    [SupportTab]
    public partial int WarrantyMonths { get; }

    [UCFProperty("Return window days", "codeFirstInteger", SortOrder = 51)]
    [SupportTab]
    public partial int ReturnWindowDays { get; }

    [UCFProperty("Published on", "codeFirstDate", SortOrder = 52)]
    [LifecycleTab]
    public partial DateTime PublishedOn { get; }

    [UCFProperty("Unpublished on", "codeFirstDate", SortOrder = 53)]
    [LifecycleTab]
    public partial DateTime UnpublishedOn { get; }

    [UCFProperty("Last reviewed on", "codeFirstDate", SortOrder = 54)]
    [LifecycleTab]
    public partial DateTime LastReviewedOn { get; }

    [UCFProperty("Reviewer", "codeFirstText", SortOrder = 55)]
    [LifecycleTab]
    public partial string Reviewer { get; }

    [UCFProperty("Featured", "codeFirstBoolean", SortOrder = 56)]
    [LifecycleTab]
    public partial bool Featured { get; }

    [UCFProperty("Archived", "codeFirstBoolean", SortOrder = 57)]
    [LifecycleTab]
    public partial bool Archived { get; }

    [UCFProperty("External reference", "codeFirstText", SortOrder = 58)]
    [LifecycleTab]
    public partial string ExternalReference { get; }

    [UCFProperty("Import batch", "codeFirstText", SortOrder = 59)]
    [LifecycleTab]
    public partial string ImportBatch { get; }

    [UCFProperty("Content owner", "codeFirstText", SortOrder = 60)]
    [LifecycleTab]
    public partial string ContentOwner { get; }
}

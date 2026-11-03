using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "productPage",
    "Product Page",
    Icon = "icon-barcode",
    AllowAsRoot = true,
    IsListView = true,
    Variations = "Invariant")]
[UCFComposite(typeof(HappySparkles))]
[UCFComposite(typeof(SeoComposite))]
[UCFDescription("A deliberately comprehensive product page used to exercise the code-first model generator.")]
public partial class ProductPage {
    [UCFProperty("productName", "Product name", "codeFirstText", SortOrder = 0, Mandatory = true, LabelOnTop = true)]
    [UCFDescription("The customer-facing product name.")]
    [ProductDetailsTab]
    [UCFValidation(@"^.{3,120}$", "The product name must contain between 3 and 120 characters.")]
    public partial string ProductName { get; }

    [UCFProperty("shortDescription", "Short description", "codeFirstText", SortOrder = 1, Mandatory = true)]
    [ProductDetailsTab]
    [UCFValidation(@"^.{10,255}$", "Enter a short description between 10 and 255 characters.")]
    public partial string ShortDescription { get; }

    [UCFProperty("description", "Description", CodeFirstLongTextDataType.Alias, SortOrder = 2)]
    [ProductDetailsTab]
    public partial string Description { get; }

    [UCFProperty("sku", "SKU", "codeFirstText", SortOrder = 3, Mandatory = true)]
    [ProductDetailsTab]
    [UCFValidation(@"^[A-Z0-9-]{4,32}$", "Use 4-32 uppercase letters, numbers, or hyphens.")]
    public partial string Sku { get; }

    [UCFProperty("brand", "Brand", "codeFirstText", SortOrder = 4)]
    [ProductDetailsTab]
    public partial string Brand { get; }

    [UCFProperty("manufacturer", "Manufacturer", "codeFirstText", SortOrder = 5)]
    [ProductDetailsTab]
    public partial string Manufacturer { get; }

    [UCFProperty("modelNumber", "Model number", "codeFirstText", SortOrder = 6)]
    [ProductDetailsTab]
    public partial string ModelNumber { get; }

    [UCFProperty("condition", "Condition", "codeFirstText", SortOrder = 7)]
    [ProductDetailsTab]
    public partial string Condition { get; }

    [UCFProperty("productCode", "Product code", "codeFirstText", SortOrder = 8)]
    [ProductDetailsTab]
    public partial string ProductCode { get; }

    [UCFProperty("barcode", "Barcode", "codeFirstText", SortOrder = 9)]
    [ProductDetailsTab]
    [UCFValidation(@"^[0-9]{8,14}$", "Enter an 8-14 digit barcode.")]
    public partial string Barcode { get; }

    [UCFProperty("price", "Price", "codeFirstDecimal", SortOrder = 10, Mandatory = true)]
    [PricingTab]
    [PricingGroup]
    [UCFValidation(@"^[0-9]+([.,][0-9]{1,2})?$", "Enter a non-negative price with up to two decimals.")]
    public partial decimal Price { get; }

    [UCFProperty("salePrice", "Sale price", "codeFirstDecimal", SortOrder = 11)]
    [PricingTab]
    [PricingGroup]
    public partial decimal SalePrice { get; }

    [UCFProperty("costPrice", "Cost price", "codeFirstDecimal", SortOrder = 12)]
    [PricingTab]
    [PricingGroup]
    public partial decimal CostPrice { get; }

    [UCFProperty("taxRate", "Tax rate", "codeFirstDecimal", SortOrder = 13)]
    [PricingTab]
    [PricingGroup]
    public partial decimal TaxRate { get; }

    [UCFProperty("currency", "Currency", "codeFirstText", SortOrder = 14)]
    [PricingTab]
    [PricingGroup]
    [UCFValidation(@"^[A-Z]{3}$", "Use a three-letter ISO currency code.")]
    public partial string Currency { get; }

    [UCFProperty("discountPercent", "Discount percent", "codeFirstDecimal", SortOrder = 15)]
    [PricingTab]
    [PricingGroup]
    public partial decimal DiscountPercent { get; }

    [UCFProperty("priceLabel", "Price label", "codeFirstText", SortOrder = 16)]
    [PricingTab]
    [PricingGroup]
    public partial string PriceLabel { get; }

    [UCFProperty("vatIncluded", "VAT included", "codeFirstBoolean", SortOrder = 17)]
    [PricingTab]
    [PricingGroup]
    public partial bool VatIncluded { get; }

    [UCFProperty("subscription", "Subscription product", "codeFirstBoolean", SortOrder = 18)]
    [PricingTab]
    [PricingGroup]
    public partial bool Subscription { get; }

    [UCFProperty("priceNote", "Price note", "codeFirstText", SortOrder = 19)]
    [PricingTab]
    [PricingGroup]
    public partial string PriceNote { get; }

    [UCFProperty("stockQuantity", "Stock quantity", "codeFirstInteger", SortOrder = 20, Mandatory = true)]
    [InventoryTab]
    public partial int StockQuantity { get; }

    [UCFProperty("lowStockThreshold", "Low stock threshold", "codeFirstInteger", SortOrder = 21)]
    [InventoryTab]
    public partial int LowStockThreshold { get; }

    [UCFProperty("backorderAllowed", "Backorders allowed", "codeFirstBoolean", SortOrder = 22)]
    [InventoryTab]
    public partial bool BackorderAllowed { get; }

    [UCFProperty("warehouseCode", "Warehouse code", "codeFirstText", SortOrder = 23)]
    [InventoryTab]
    public partial string WarehouseCode { get; }

    [UCFProperty("aisle", "Aisle", "codeFirstText", SortOrder = 24)]
    [InventoryTab]
    public partial string Aisle { get; }

    [UCFProperty("bin", "Bin", "codeFirstText", SortOrder = 25)]
    [InventoryTab]
    public partial string Bin { get; }

    [UCFProperty("restockDate", "Restock date", "codeFirstDate", SortOrder = 26)]
    [InventoryTab]
    public partial DateTime RestockDate { get; }

    [UCFProperty("weightGrams", "Weight in grams", "codeFirstInteger", SortOrder = 27)]
    [InventoryTab]
    public partial int WeightGrams { get; }

    [UCFProperty("lengthMillimeters", "Length in millimeters", "codeFirstInteger", SortOrder = 28)]
    [InventoryTab]
    public partial int LengthMillimeters { get; }

    [UCFProperty("widthMillimeters", "Width in millimeters", "codeFirstInteger", SortOrder = 29)]
    [InventoryTab]
    public partial int WidthMillimeters { get; }

    [UCFProperty("heightMillimeters", "Height in millimeters", "codeFirstInteger", SortOrder = 30)]
    [InventoryTab]
    public partial int HeightMillimeters { get; }

    [UCFProperty("heroTitle", "Hero title", "codeFirstText", SortOrder = 31)]
    [MarketingTab]
    public partial string HeroTitle { get; }

    [UCFProperty("heroSubtitle", "Hero subtitle", "codeFirstText", SortOrder = 32)]
    [MarketingTab]
    public partial string HeroSubtitle { get; }

    [UCFProperty("callToActionLabel", "Call to action label", "codeFirstText", SortOrder = 33)]
    [MarketingTab]
    public partial string CallToActionLabel { get; }

    [UCFProperty("callToActionUrl", "Call to action URL", "codeFirstText", SortOrder = 34)]
    [MarketingTab]
    [UCFValidation(@"^https?://", "Enter an absolute HTTP or HTTPS URL.")]
    public partial string CallToActionUrl { get; }

    [UCFProperty("marketingBadge", "Marketing badge", "codeFirstText", SortOrder = 35)]
    [MarketingTab]
    public partial string MarketingBadge { get; }

    [UCFProperty("seoTitle", "SEO title", "codeFirstText", SortOrder = 36)]
    [SeoTab]
    [UCFValidation(@"^.{0,60}$", "The SEO title must be at most 60 characters.")]
    public partial string SeoTitle { get; }

    [UCFProperty("seoDescription", "SEO description", "codeFirstLongText", SortOrder = 37)]
    [SeoTab]
    [UCFValidation(@"^.{0,160}$", "The SEO description must be at most 160 characters.")]
    public partial string SeoDescription { get; }

    [UCFProperty("seoKeywords", "SEO keywords", "codeFirstText", SortOrder = 38)]
    [SeoTab]
    public partial string SeoKeywords { get; }

    [UCFProperty("canonicalUrl", "Canonical URL", "codeFirstText", SortOrder = 39)]
    [SeoTab]
    public partial string CanonicalUrl { get; }

    [UCFProperty("noIndex", "No index", "codeFirstBoolean", SortOrder = 40)]
    [SeoTab]
    public partial bool NoIndex { get; }

    [UCFProperty("openGraphTitle", "Open Graph title", "codeFirstText", SortOrder = 41)]
    [SeoTab]
    public partial string OpenGraphTitle { get; }

    [UCFProperty("openGraphDescription", "Open Graph description", "codeFirstLongText", SortOrder = 42)]
    [SeoTab]
    public partial string OpenGraphDescription { get; }

    [UCFProperty("twitterTitle", "Twitter title", "codeFirstText", SortOrder = 43)]
    [SeoTab]
    public partial string TwitterTitle { get; }

    [UCFProperty("twitterDescription", "Twitter description", "codeFirstLongText", SortOrder = 44)]
    [SeoTab]
    public partial string TwitterDescription { get; }

    [UCFProperty("searchBoost", "Search boost", "codeFirstDecimal", SortOrder = 45)]
    [SeoTab]
    public partial decimal SearchBoost { get; }

    [UCFProperty("supportEmail", "Support email", "codeFirstEmail", SortOrder = 46)]
    [SupportTab]
    [UCFValidation(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Enter a valid support email address.")]
    public partial string SupportEmail { get; }

    [UCFProperty("supportPhone", "Support phone", "codeFirstText", SortOrder = 47)]
    [SupportTab]
    [UCFValidation(@"^[0-9+ ()-]{7,24}$", "Enter a valid support phone number.")]
    public partial string SupportPhone { get; }

    [UCFProperty("supportHours", "Support hours", "codeFirstText", SortOrder = 48)]
    [SupportTab]
    public partial string SupportHours { get; }

    [UCFProperty("supportUrl", "Support URL", "codeFirstText", SortOrder = 49)]
    [SupportTab]
    public partial string SupportUrl { get; }

    [UCFProperty("warrantyMonths", "Warranty months", "codeFirstInteger", SortOrder = 50)]
    [SupportTab]
    public partial int WarrantyMonths { get; }

    [UCFProperty("returnWindowDays", "Return window days", "codeFirstInteger", SortOrder = 51)]
    [SupportTab]
    public partial int ReturnWindowDays { get; }

    [UCFProperty("publishedOn", "Published on", "codeFirstDate", SortOrder = 52)]
    [LifecycleTab]
    public partial DateTime PublishedOn { get; }

    [UCFProperty("unpublishedOn", "Unpublished on", "codeFirstDate", SortOrder = 53)]
    [LifecycleTab]
    public partial DateTime UnpublishedOn { get; }

    [UCFProperty("lastReviewedOn", "Last reviewed on", "codeFirstDate", SortOrder = 54)]
    [LifecycleTab]
    public partial DateTime LastReviewedOn { get; }

    [UCFProperty("reviewer", "Reviewer", "codeFirstText", SortOrder = 55)]
    [LifecycleTab]
    public partial string Reviewer { get; }

    [UCFProperty("featured", "Featured", "codeFirstBoolean", SortOrder = 56)]
    [LifecycleTab]
    public partial bool Featured { get; }

    [UCFProperty("archived", "Archived", "codeFirstBoolean", SortOrder = 57)]
    [LifecycleTab]
    public partial bool Archived { get; }

    [UCFProperty("externalReference", "External reference", "codeFirstText", SortOrder = 58)]
    [LifecycleTab]
    public partial string ExternalReference { get; }

    [UCFProperty("importBatch", "Import batch", "codeFirstText", SortOrder = 59)]
    [LifecycleTab]
    public partial string ImportBatch { get; }

    [UCFProperty("contentOwner", "Content owner", "codeFirstText", SortOrder = 60)]
    [LifecycleTab]
    public partial string ContentOwner { get; }
}

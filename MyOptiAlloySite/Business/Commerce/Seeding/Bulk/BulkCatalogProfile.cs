namespace MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

/// <summary>
/// Defines one bulk catalog dataset. Each profile owns a distinct category and a distinct
/// entry-code prefix, so datasets never overlap and can be created or removed independently.
/// </summary>
/// <param name="Key">Short identifier used in the category code and entry codes.</param>
/// <param name="CategoryName">Display name of the category the dataset lives under.</param>
/// <param name="ProductCount">Number of products to create.</param>
/// <param name="VariantsPerProduct">Variants created beneath each product.</param>
public sealed record BulkCatalogProfile(
    string Key,
    string CategoryName,
    int ProductCount,
    int VariantsPerProduct)
{
    /// <summary>Category code, e.g. <c>bulk-1k</c>.</summary>
    public string CategoryCode => $"bulk-{Key}";

    /// <summary>Prefix for every entry code in this dataset, e.g. <c>b1k</c>.</summary>
    public string CodePrefix => $"b{Key}";

    /// <summary>Products plus variants.</summary>
    public int TotalEntries => ProductCount + (ProductCount * VariantsPerProduct);

    public string ProductCode(int index) => $"{CodePrefix}-p{index:D6}";

    public string VariantCode(int productIndex, int variantIndex) =>
        $"{CodePrefix}-p{productIndex:D6}-v{variantIndex:D2}";

    /// <summary>1,000 entries: 200 products with 4 variants each.</summary>
    public static BulkCatalogProfile Small => new("1k", "Bulk 1k", 200, 4);

    /// <summary>10,000 entries: 2,000 products with 4 variants each.</summary>
    public static BulkCatalogProfile Medium => new("10k", "Bulk 10k", 2_000, 4);

    /// <summary>100,000 entries: 20,000 products with 4 variants each.</summary>
    public static BulkCatalogProfile Large => new("100k", "Bulk 100k", 20_000, 4);

    public static IReadOnlyList<BulkCatalogProfile> All => [Small, Medium, Large];
}

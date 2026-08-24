using System.Text.Json.Serialization;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Data;

/// <summary>
/// Shape of SeedData/commerce-catalog.json. Kept as plain data so the dataset can grow
/// without recompiling.
/// </summary>
public sealed class SeedDocument
{
    public SeedMarket Market { get; init; } = new();
    public SeedWarehouse Warehouse { get; init; } = new();
    public SeedCatalog Catalog { get; init; } = new();
    public IReadOnlyList<SeedCategory> Categories { get; init; } = [];
    public IReadOnlyList<SeedProduct> Products { get; init; } = [];
    public IReadOnlyList<SeedCustomer> Customers { get; init; } = [];
    public SeedCampaign Campaign { get; init; } = new();
}

public sealed class SeedMarket
{
    public string Name { get; init; } = "Default";
    public string Currency { get; init; } = "USD";
    public string Language { get; init; } = "en";
    public IReadOnlyList<string> Countries { get; init; } = [];
}

public sealed class SeedWarehouse
{
    public string Code { get; init; } = "default";
    public string Name { get; init; } = "Default warehouse";
}

public sealed class SeedCatalog
{
    public string Name { get; init; } = "Catalog";
}

public sealed class SeedCategory
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Teaser { get; init; } = "";
}

public sealed class SeedProduct
{
    public string Code { get; init; } = "";
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public string Teaser { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<string> SellingPoints { get; init; } = [];
    public IReadOnlyList<SeedVariant> Variants { get; init; } = [];
}

public sealed class SeedVariant
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Tier { get; init; } = "";
    public string Billing { get; init; } = "";
    public int Seats { get; init; }
    public decimal Price { get; init; }

    /// <summary>Null means "not stock tracked".</summary>
    public decimal? Stock { get; init; }
}

public sealed class SeedCustomer
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Organization { get; init; } = "";
    public string City { get; init; } = "";
    public string CountryCode { get; init; } = "";
}

public sealed class SeedCampaign
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<SeedPromotion> Promotions { get; init; } = [];
}

public sealed class SeedPromotion
{
    public string Name { get; init; } = "";

    /// <summary>Order total the customer must reach for the discount to apply.</summary>
    public decimal SpendThreshold { get; init; }

    /// <summary>Percentage taken off the order total.</summary>
    public decimal PercentOff { get; init; }
}

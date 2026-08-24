using MyOptiAlloySite.Business.Commerce.Seeding;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;
using MyOptiAlloySite.Business.Commerce.Seeding.Steps;

namespace MyOptiAlloySite.Extensions;

public static class CommerceSeedingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the commerce seed dataset and its steps. Steps are resolved as a set and
    /// ordered by <see cref="ISeedStep.Order"/>, so adding one here is enough to put it in
    /// the pipeline.
    /// </summary>
    public static IServiceCollection AddCommerceSeeding(this IServiceCollection services)
    {
        services.AddSingleton<SeedDataProvider>();
        services.AddTransient<BulkCatalogSeeder>();

        services.AddTransient<ISeedStep, FoundationSeedStep>();
        services.AddTransient<ISeedStep, CatalogSeedStep>();
        services.AddTransient<ISeedStep, ProductSeedStep>();
        services.AddTransient<ISeedStep, PriceSeedStep>();
        services.AddTransient<ISeedStep, InventorySeedStep>();
        services.AddTransient<ISeedStep, CatalogAssetSeedStep>();
        services.AddTransient<ISeedStep, AssociationSeedStep>();
        services.AddTransient<ISeedStep, CustomerSeedStep>();
        services.AddTransient<ISeedStep, MarketingSeedStep>();
        services.AddTransient<ISeedStep, OrderSeedStep>();
        services.AddTransient<ISeedStep, SearchIndexSeedStep>();

        return services;
    }
}

using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.DataAccess;
using EPiServer.Security;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Inventory;
using Mediachase.Commerce.InventoryService;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using MyOptiAlloySite.Models.Catalog;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

/// <summary>
/// Creates and removes large synthetic catalog datasets for load and performance testing.
/// Kept out of the normal seed pipeline: these run for minutes to hours and should only
/// happen when someone asks for them.
/// </summary>
public sealed class BulkCatalogSeeder(
    IContentRepository contentRepository,
    IContentLoader contentLoader,
    ReferenceConverter referenceConverter,
    IPriceDetailService priceDetailService,
    IInventoryService inventoryService,
    IWarehouseRepository warehouseRepository,
    IMarketService marketService)
{
    /// <summary>Catalog the bulk categories are created under.</summary>
    public const string CatalogName = "Alloy Catalog";

    /// <summary>Prices and inventory are written in batches of this size.</summary>
    private const int BatchSize = 500;

    /// <summary>How often progress is reported, in products.</summary>
    private const int ReportEvery = 100;

    public SeedOutcome Seed(BulkCatalogProfile profile, SeedRunContext context)
    {
        var market = marketService.GetMarket(MarketId.Default)
            ?? throw new InvalidOperationException(
                "No default market. Run 'Seed Commerce data' first, or create a market in the Commerce admin.");

        var currency = market.DefaultCurrency;
        var warehouseCode = warehouseRepository.GetDefaultWarehouse()?.Code;

        var catalogLink = EnsureCatalog(context);
        var categoryLink = EnsureCategory(profile, catalogLink, context);

        // A fully complete dataset ends with the last variant of the last product. Checking
        // that one code is both cheaper and stricter than counting products, which would
        // call a half-written final product "done".
        if (Exists(profile.VariantCode(profile.ProductCount - 1, profile.VariantsPerProduct - 1)))
        {
            context.Report($"'{profile.CategoryName}' already holds all {profile.TotalEntries:N0} entries.");
            return SeedOutcome.Empty.WithUnchanged();
        }

        var firstMissing = FindResumePoint(profile);

        // Back up far enough to cover a product that was half written when a previous run
        // died, plus any price and inventory batch that had not been flushed yet. Entries in
        // this window are checked individually instead of assumed absent.
        var productsPerBatch = (BatchSize / profile.VariantsPerProduct) + 1;
        var resumeFrom = Math.Max(0, firstMissing - productsPerBatch);

        if (resumeFrom > 0)
        {
            context.Report(
                $"Resuming '{profile.CategoryName}' at product {resumeFrom:N0} " +
                $"(first missing is {firstMissing:N0}; re-checking the preceding batch).");
        }

        var outcome = SeedOutcome.Empty;
        var prices = new List<IPriceDetailValue>(BatchSize);
        var inventory = new List<InventoryRecord>(BatchSize);

        for (var i = resumeFrom; i < profile.ProductCount; i++)
        {
            context.ThrowIfStopRequested();

            // Everything from firstMissing onwards is known absent, so it can be created
            // without a lookup. Only the re-checked window pays for existence probes.
            var mayExist = i < firstMissing;
            var productLink = mayExist
                ? referenceConverter.GetContentLink(profile.ProductCode(i), CatalogContentType.CatalogEntry)
                : ContentReference.EmptyReference;

            if (ContentReference.IsNullOrEmpty(productLink))
            {
                productLink = CreateProduct(profile, categoryLink, i);
                outcome = outcome.WithCreated();
            }

            for (var v = 0; v < profile.VariantsPerProduct; v++)
            {
                var code = profile.VariantCode(i, v);

                if (!mayExist || !Exists(code))
                {
                    CreateVariant(profile, productLink, code, i, v);
                    outcome = outcome.WithCreated();
                }

                // Prices and inventory are upserts, so they are re-sent for the re-checked
                // window: the batch covering those entries may never have been flushed.
                var price = 10m + ((i * profile.VariantsPerProduct + v) % 500);

                prices.Add(new PriceDetailValue
                {
                    CatalogKey = new CatalogKey(code),
                    MarketId = MarketId.Default,
                    CustomerPricing = CustomerPricing.AllCustomers,
                    ValidFrom = DateTime.UtcNow.Date,
                    MinQuantity = 0,
                    UnitPrice = new Money(price, currency),
                });

                if (warehouseCode is not null)
                {
                    inventory.Add(new InventoryRecord
                    {
                        CatalogEntryCode = code,
                        WarehouseCode = warehouseCode,
                        IsTracked = true,
                        PurchaseAvailableQuantity = 100 + (i % 400),
                        PurchaseAvailableUtc = DateTime.UtcNow.Date,
                    });
                }
            }

            if (prices.Count >= BatchSize)
            {
                FlushBatches(prices, inventory);
            }

            if ((i - resumeFrom + 1) % ReportEvery == 0)
            {
                context.Report($"{profile.CategoryName}: {i + 1:N0}/{profile.ProductCount:N0} products.");
            }
        }

        FlushBatches(prices, inventory);
        context.Report($"{profile.CategoryName}: {outcome.Created:N0} entries created.");
        return outcome;
    }

    public SeedOutcome Remove(BulkCatalogProfile profile, SeedRunContext context)
    {
        var link = referenceConverter.GetContentLink(profile.CategoryCode, CatalogContentType.CatalogNode);

        if (ContentReference.IsNullOrEmpty(link))
        {
            context.Report($"'{profile.CategoryName}' not present.");
            return SeedOutcome.Empty;
        }

        // Deleting the category takes its products, variants, prices and inventory with it.
        contentRepository.Delete(link, forceDelete: true, AccessLevel.NoAccess);
        context.Report($"Removed '{profile.CategoryName}' and everything under it.");
        return SeedOutcome.Empty.WithRemoved();
    }

    private void FlushBatches(List<IPriceDetailValue> prices, List<InventoryRecord> inventory)
    {
        if (prices.Count > 0)
        {
            priceDetailService.Save(prices);
            prices.Clear();
        }

        if (inventory.Count > 0)
        {
            inventoryService.Save(inventory);
            inventory.Clear();
        }
    }

    private ContentReference CreateProduct(BulkCatalogProfile profile, ContentReference categoryLink, int index)
    {
        var product = contentRepository.GetDefault<AlloyProduct>(categoryLink);
        product.Code = profile.ProductCode(index);
        product.Name = $"{profile.CategoryName} product {index:N0}";
        product.DisplayName = product.Name;
        product.TeaserText = $"Synthetic product {index:N0} in the {profile.CategoryName} dataset.";

        return contentRepository.Save(product, SaveAction.Publish, AccessLevel.NoAccess);
    }

    private void CreateVariant(
        BulkCatalogProfile profile, ContentReference productLink, string code, int productIndex, int variantIndex)
    {
        var variant = contentRepository.GetDefault<AlloyVariant>(productLink);
        variant.Code = code;
        variant.Name = $"{profile.CategoryName} product {productIndex:N0} variant {variantIndex + 1}";
        variant.DisplayName = variant.Name;
        variant.LicenceTier = $"Tier {variantIndex + 1}";
        variant.BillingPeriod = variantIndex % 2 == 0 ? "Monthly" : "Annual";
        variant.SeatCount = (variantIndex + 1) * 5;

        contentRepository.Save(variant, SaveAction.Publish, AccessLevel.NoAccess);
    }

    /// <summary>
    /// Index of the first product not yet created. Products are always created in order,
    /// so the existing range is contiguous and a binary search finds the boundary in
    /// log(n) lookups instead of probing all of them.
    /// </summary>
    private int FindResumePoint(BulkCatalogProfile profile)
    {
        if (Exists(profile.ProductCode(profile.ProductCount - 1)))
        {
            return profile.ProductCount;
        }

        if (!Exists(profile.ProductCode(0)))
        {
            return 0;
        }

        var present = 0;
        var missing = profile.ProductCount - 1;

        while (missing - present > 1)
        {
            var mid = present + ((missing - present) / 2);
            if (Exists(profile.ProductCode(mid)))
            {
                present = mid;
            }
            else
            {
                missing = mid;
            }
        }

        return missing;
    }

    private bool Exists(string code) =>
        !ContentReference.IsNullOrEmpty(referenceConverter.GetContentLink(code, CatalogContentType.CatalogEntry));

    private ContentReference EnsureCatalog(SeedRunContext context)
    {
        var rootLink = referenceConverter.GetRootLink();
        var existing = contentLoader.GetChildren<CatalogContent>(rootLink)
            .FirstOrDefault(c => string.Equals(c.Name, CatalogName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing.ContentLink;
        }

        var catalog = contentRepository.GetDefault<CatalogContent>(rootLink);
        catalog.Name = CatalogName;
        catalog.DefaultCurrency = marketService.GetMarket(MarketId.Default).DefaultCurrency.CurrencyCode;
        catalog.DefaultLanguage = "en";
        catalog.WeightBase = "kgs";
        catalog.LengthBase = "cm";
        catalog.IsPrimary = true;
        catalog.CatalogLanguages.Add("en");

        context.Report($"Created catalog '{CatalogName}'.");
        return contentRepository.Save(catalog, SaveAction.Publish, AccessLevel.NoAccess);
    }

    private ContentReference EnsureCategory(
        BulkCatalogProfile profile, ContentReference catalogLink, SeedRunContext context)
    {
        var link = referenceConverter.GetContentLink(profile.CategoryCode, CatalogContentType.CatalogNode);
        if (!ContentReference.IsNullOrEmpty(link))
        {
            return link;
        }

        var category = contentRepository.GetDefault<AlloyCategory>(catalogLink);
        category.Code = profile.CategoryCode;
        category.Name = profile.CategoryName;
        category.DisplayName = profile.CategoryName;
        category.TeaserText = $"Synthetic dataset of {profile.TotalEntries:N0} catalog entries for load testing.";

        context.Report($"Created category '{profile.CategoryCode}'.");
        return contentRepository.Save(category, SaveAction.Publish, AccessLevel.NoAccess);
    }
}

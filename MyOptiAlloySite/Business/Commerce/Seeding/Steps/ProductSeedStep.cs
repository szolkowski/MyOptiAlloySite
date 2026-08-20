using EPiServer.DataAccess;
using EPiServer.Security;
using Mediachase.Commerce.Catalog;
using MyOptiAlloySite.Business.Commerce.Seeding.Data;
using MyOptiAlloySite.Models.Catalog;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Products and their variants. A variant's parent is its product, which is what makes
/// it a variant of that product rather than a loose entry under the category.
/// </summary>
public sealed class ProductSeedStep(
    IContentRepository contentRepository,
    IContentLoader contentLoader,
    ReferenceConverter referenceConverter,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 300;

    public string Name => "Products and variants";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var outcome = SeedOutcome.Empty;

        foreach (var product in data.Products)
        {
            context.ThrowIfStopRequested();

            var categoryLink = referenceConverter.GetContentLink(product.Category, CatalogContentType.CatalogNode);
            if (ContentReference.IsNullOrEmpty(categoryLink))
            {
                throw new InvalidOperationException(
                    $"Product '{product.Code}' expects category '{product.Category}', which does not exist. " +
                    "Run the 'Catalog and categories' step first.");
            }

            var (productLink, productOutcome) = EnsureProduct(context, categoryLink, product);
            outcome = outcome.Add(productOutcome);

            foreach (var variant in product.Variants)
            {
                context.ThrowIfStopRequested();
                outcome = outcome.Add(EnsureVariant(context, productLink, variant));
            }
        }

        return outcome;
    }

    private (ContentReference ProductLink, SeedOutcome Outcome) EnsureProduct(
        SeedRunContext context, ContentReference categoryLink, SeedProduct seed)
    {
        var sellingPoints = string.Join(Environment.NewLine, seed.SellingPoints);
        var link = referenceConverter.GetContentLink(seed.Code, CatalogContentType.CatalogEntry);

        if (!ContentReference.IsNullOrEmpty(link) && contentLoader.TryGet<AlloyProduct>(link, out var existing))
        {
            if (existing.DisplayName == seed.Name
                && existing.TeaserText == seed.Teaser
                && existing.UniqueSellingPoints == sellingPoints)
            {
                return (existing.ContentLink, SeedOutcome.Empty.WithUnchanged());
            }

            var writable = existing.CreateWritableClone<AlloyProduct>();
            ApplyProduct(writable, seed, sellingPoints);
            var updatedLink = contentRepository.Save(writable, SaveAction.Publish, AccessLevel.NoAccess);
            context.Report($"Updated product '{seed.Code}'.");
            return (updatedLink, SeedOutcome.Empty.WithUpdated());
        }

        var product = contentRepository.GetDefault<AlloyProduct>(categoryLink);
        product.Code = seed.Code;
        ApplyProduct(product, seed, sellingPoints);

        var newLink = contentRepository.Save(product, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created product '{seed.Code}'.");
        return (newLink, SeedOutcome.Empty.WithCreated());
    }

    private static void ApplyProduct(AlloyProduct product, SeedProduct seed, string sellingPoints)
    {
        product.Name = seed.Name;
        product.DisplayName = seed.Name;
        product.TeaserText = seed.Teaser;
        product.Description = new XhtmlString(seed.Description);
        product.UniqueSellingPoints = sellingPoints;
    }

    private SeedOutcome EnsureVariant(SeedRunContext context, ContentReference productLink, SeedVariant seed)
    {
        var link = referenceConverter.GetContentLink(seed.Code, CatalogContentType.CatalogEntry);

        if (!ContentReference.IsNullOrEmpty(link) && contentLoader.TryGet<AlloyVariant>(link, out var existing))
        {
            if (existing.DisplayName == seed.Name
                && existing.LicenceTier == seed.Tier
                && existing.BillingPeriod == seed.Billing
                && existing.SeatCount == seed.Seats)
            {
                return SeedOutcome.Empty.WithUnchanged();
            }

            var writable = existing.CreateWritableClone<AlloyVariant>();
            ApplyVariant(writable, seed);
            contentRepository.Save(writable, SaveAction.Publish, AccessLevel.NoAccess);
            context.Report($"Updated variant '{seed.Code}'.");
            return SeedOutcome.Empty.WithUpdated();
        }

        var variant = contentRepository.GetDefault<AlloyVariant>(productLink);
        variant.Code = seed.Code;
        ApplyVariant(variant, seed);

        contentRepository.Save(variant, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created variant '{seed.Code}'.");
        return SeedOutcome.Empty.WithCreated();
    }

    private static void ApplyVariant(AlloyVariant variant, SeedVariant seed)
    {
        variant.Name = seed.Name;
        variant.DisplayName = seed.Name;
        variant.LicenceTier = seed.Tier;
        variant.BillingPeriod = seed.Billing;
        variant.SeatCount = seed.Seats;
    }

    /// <summary>
    /// No-op: products and variants live inside the catalog, and removing the catalog
    /// takes them with it.
    /// </summary>
    public SeedOutcome Remove(SeedRunContext context) => SeedOutcome.Empty;
}

using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.DataAccess;
using EPiServer.Security;
using Mediachase.Commerce.Catalog;
using MyOptiAlloySite.Models.Catalog;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// The catalog itself plus its top-level category nodes.
/// </summary>
public sealed class CatalogSeedStep(
    IContentRepository contentRepository,
    IContentLoader contentLoader,
    ReferenceConverter referenceConverter,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 200;

    public string Name => "Catalog and categories";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var (catalogLink, outcome) = EnsureCatalog(context, data.Catalog.Name, data.Market.Currency, data.Market.Language);

        foreach (var category in data.Categories)
        {
            context.ThrowIfStopRequested();
            outcome = outcome.Add(EnsureCategory(context, catalogLink, category.Code, category.Name, category.Teaser));
        }

        return outcome;
    }

    private (ContentReference CatalogLink, SeedOutcome Outcome) EnsureCatalog(
        SeedRunContext context, string name, string currency, string language)
    {
        var rootLink = referenceConverter.GetRootLink();
        var existing = contentLoader.GetChildren<CatalogContent>(rootLink)
            .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            context.Report($"Catalog '{name}' already exists.");
            return (existing.ContentLink, SeedOutcome.Empty.WithUnchanged());
        }

        var catalog = contentRepository.GetDefault<CatalogContent>(rootLink);
        catalog.Name = name;
        catalog.DefaultCurrency = currency;
        catalog.DefaultLanguage = language;
        catalog.WeightBase = "kgs";
        catalog.LengthBase = "cm";
        catalog.IsPrimary = true;
        catalog.CatalogLanguages.Add(language);

        var link = contentRepository.Save(catalog, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created catalog '{name}'.");
        return (link, SeedOutcome.Empty.WithCreated());
    }

    private SeedOutcome EnsureCategory(
        SeedRunContext context, ContentReference catalogLink, string code, string name, string teaser)
    {
        var link = referenceConverter.GetContentLink(code, CatalogContentType.CatalogNode);

        if (!ContentReference.IsNullOrEmpty(link) && contentLoader.TryGet<AlloyCategory>(link, out var existing))
        {
            if (existing.DisplayName == name && existing.TeaserText == teaser)
            {
                return SeedOutcome.Empty.WithUnchanged();
            }

            var writable = existing.CreateWritableClone<AlloyCategory>();
            writable.DisplayName = name;
            writable.TeaserText = teaser;
            contentRepository.Save(writable, SaveAction.Publish, AccessLevel.NoAccess);
            context.Report($"Updated category '{code}'.");
            return SeedOutcome.Empty.WithUpdated();
        }

        var category = contentRepository.GetDefault<AlloyCategory>(catalogLink);
        category.Code = code;
        category.Name = name;
        category.DisplayName = name;
        category.TeaserText = teaser;

        contentRepository.Save(category, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created category '{code}'.");
        return SeedOutcome.Empty.WithCreated();
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var rootLink = referenceConverter.GetRootLink();
        var catalog = contentLoader.GetChildren<CatalogContent>(rootLink)
            .FirstOrDefault(c => string.Equals(c.Name, data.Catalog.Name, StringComparison.OrdinalIgnoreCase));

        if (catalog is null)
        {
            context.Report($"Catalog '{data.Catalog.Name}' not present.");
            return SeedOutcome.Empty;
        }

        // Deleting the catalog takes its categories and entries with it.
        contentRepository.Delete(catalog.ContentLink, forceDelete: true, AccessLevel.NoAccess);
        context.Report($"Removed catalog '{data.Catalog.Name}' and everything under it.");
        return SeedOutcome.Empty.WithRemoved();
    }
}

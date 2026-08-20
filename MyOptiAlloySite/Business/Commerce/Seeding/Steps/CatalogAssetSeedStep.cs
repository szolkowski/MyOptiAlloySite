using EPiServer.Commerce.SpecializedProperties;
using EPiServer.DataAccess;
using EPiServer.Security;
using Mediachase.Commerce.Catalog;
using MyOptiAlloySite.Models.Catalog;
using EPiServer.Web;
using MyOptiAlloySite.Models.Media;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Attaches existing Alloy imagery to seeded products, so the catalog UI shows real
/// thumbnails rather than placeholders. Reuses the site's own media rather than importing
/// new files.
/// </summary>
public sealed class CatalogAssetSeedStep(
    IContentRepository contentRepository,
    IContentLoader contentLoader,
    ReferenceConverter referenceConverter,
    SeedDataProvider dataProvider) : ISeedStep
{
    private const string GroupName = "default";

    /// <summary>
    /// Commerce stores the asset's content interface here and rejects a null, so it must
    /// be set explicitly even though the collection would otherwise accept the media.
    /// </summary>
    private static readonly string ImageAssetType = typeof(IContentImage).FullName!.ToLowerInvariant();

    public int Order => 550;

    public string Name => "Catalog assets";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var images = FindSiteImages();

        if (images.Count == 0)
        {
            context.Report("No ImageFile media found in the site; leaving products without assets.");
            return SeedOutcome.Empty;
        }

        var outcome = SeedOutcome.Empty;
        var index = 0;

        foreach (var seed in data.Products)
        {
            context.ThrowIfStopRequested();

            var link = referenceConverter.GetContentLink(seed.Code, CatalogContentType.CatalogEntry);
            if (ContentReference.IsNullOrEmpty(link) || !contentLoader.TryGet<AlloyProduct>(link, out var product))
            {
                continue;
            }

            // Deterministic pick so a re-run after teardown produces the same pairing.
            var image = images[index++ % images.Count];

            if (product.CommerceMediaCollection.Any(m => m.AssetLink.ID == image.ID))
            {
                outcome = outcome.WithUnchanged();
                continue;
            }

            var writable = product.CreateWritableClone<AlloyProduct>();
            writable.CommerceMediaCollection.Add(new CommerceMedia
            {
                AssetLink = image,
                AssetType = ImageAssetType,
                GroupName = GroupName,
                SortOrder = 0,
            });

            contentRepository.Save(writable, SaveAction.Publish, AccessLevel.NoAccess);
            context.Report($"Attached image to product '{seed.Code}'.");
            outcome = outcome.WithCreated();
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        // No-op: assets are attached to products, and removing the catalog removes them.
        // The images themselves belong to the site and are never ours to delete.
        return SeedOutcome.Empty;
    }

    /// <summary>
    /// Images the site already owns, taken from the global assets root. Uses
    /// <see cref="SystemDefinition"/> rather than the obsolete SiteDefinition roots.
    /// </summary>
    private List<ContentReference> FindSiteImages() =>
        contentLoader.GetDescendents(SystemDefinition.Current.GlobalAssetsRoot)
            .Where(link => contentLoader.TryGet<ImageFile>(link, out _))
            .Distinct()
            .ToList();
}

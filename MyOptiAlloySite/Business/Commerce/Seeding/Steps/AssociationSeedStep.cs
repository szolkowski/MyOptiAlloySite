using EPiServer.Commerce.Catalog.Linking;
using Mediachase.Commerce.Catalog;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Cross-sell associations between products in the same category, so related-product
/// views have something to show.
/// </summary>
public sealed class AssociationSeedStep(
    IAssociationRepository associationRepository,
    ReferenceConverter referenceConverter,
    SeedDataProvider dataProvider) : ISeedStep
{
    private const string GroupName = "cross-sell";

    public int Order => 600;

    public string Name => "Product associations";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var outcome = SeedOutcome.Empty;

        foreach (var group in data.Products.GroupBy(p => p.Category))
        {
            var siblings = group.ToList();
            if (siblings.Count < 2)
            {
                continue;
            }

            foreach (var product in siblings)
            {
                context.ThrowIfStopRequested();

                var sourceLink = referenceConverter.GetContentLink(product.Code, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(sourceLink))
                {
                    throw new InvalidOperationException(
                        $"No catalog entry for product '{product.Code}'. Run the 'Products and variants' step first.");
                }

                var existing = associationRepository.GetAssociations(sourceLink).ToList();
                var toAdd = new List<Association>();
                var sortOrder = 0;

                foreach (var target in siblings.Where(s => s.Code != product.Code))
                {
                    var targetLink = referenceConverter.GetContentLink(target.Code, CatalogContentType.CatalogEntry);
                    if (ContentReference.IsNullOrEmpty(targetLink))
                    {
                        continue;
                    }

                    if (existing.Any(a => a.Target.ID == targetLink.ID && a.Group?.Name == GroupName))
                    {
                        outcome = outcome.WithUnchanged();
                        continue;
                    }

                    toAdd.Add(new Association
                    {
                        Source = sourceLink,
                        Target = targetLink,
                        SortOrder = sortOrder++,
                        Group = new AssociationGroup { Name = GroupName, SortOrder = 0 },
                        Type = new AssociationType { Id = AssociationType.DefaultTypeId },
                    });
                    outcome = outcome.WithCreated();
                }

                if (toAdd.Count > 0)
                {
                    associationRepository.UpdateAssociations(toAdd);
                    context.Report($"Added {toAdd.Count} association(s) for '{product.Code}'.");
                }
            }
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var toRemove = new List<Association>();

        foreach (var product in data.Products)
        {
            context.ThrowIfStopRequested();

            var sourceLink = referenceConverter.GetContentLink(product.Code, CatalogContentType.CatalogEntry);
            if (ContentReference.IsNullOrEmpty(sourceLink))
            {
                continue;
            }

            toRemove.AddRange(associationRepository.GetAssociations(sourceLink)
                .Where(a => a.Group?.Name == GroupName));
        }

        if (toRemove.Count == 0)
        {
            context.Report("No seeded associations present.");
            return SeedOutcome.Empty;
        }

        associationRepository.RemoveAssociations(toRemove);
        context.Report($"Removed {toRemove.Count} association(s).");
        return SeedOutcome.Empty.WithRemoved(toRemove.Count);
    }
}

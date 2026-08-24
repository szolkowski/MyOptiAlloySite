using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// List prices for every seeded variant, in the default market's currency.
/// </summary>
public sealed class PriceSeedStep(
    IPriceDetailService priceDetailService,
    ReferenceConverter referenceConverter,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 400;

    public string Name => "Prices";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var currency = new Currency(data.Market.Currency);
        var outcome = SeedOutcome.Empty;
        var toSave = new List<IPriceDetailValue>();

        foreach (var variant in data.Products.SelectMany(p => p.Variants))
        {
            context.ThrowIfStopRequested();

            var link = referenceConverter.GetContentLink(variant.Code, CatalogContentType.CatalogEntry);
            if (ContentReference.IsNullOrEmpty(link))
            {
                throw new InvalidOperationException(
                    $"No catalog entry for variant '{variant.Code}'. Run the 'Products and variants' step first.");
            }

            var existing = priceDetailService.List(link)
                .FirstOrDefault(p => p.MarketId == MarketId.Default
                    && p.UnitPrice.Currency == currency
                    && p.MinQuantity == 0
                    && Equals(p.CustomerPricing, CustomerPricing.AllCustomers));

            if (existing is not null)
            {
                if (existing.UnitPrice.Amount == variant.Price)
                {
                    outcome = outcome.WithUnchanged();
                    continue;
                }

                // Carries PriceValueId, so this updates the row rather than adding another.
                toSave.Add(new PriceDetailValue(existing) { UnitPrice = new Money(variant.Price, currency) });
                outcome = outcome.WithUpdated();
                continue;
            }

            toSave.Add(new PriceDetailValue
            {
                CatalogKey = new CatalogKey(variant.Code),
                MarketId = MarketId.Default,
                CustomerPricing = CustomerPricing.AllCustomers,
                ValidFrom = DateTime.UtcNow.Date,
                MinQuantity = 0,
                UnitPrice = new Money(variant.Price, currency),
            });
            outcome = outcome.WithCreated();
        }

        if (toSave.Count > 0)
        {
            priceDetailService.Save(toSave);
            context.Report($"Saved {toSave.Count} price(s) in {currency.CurrencyCode}.");
        }
        else
        {
            context.Report("All prices already up to date.");
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var ids = new List<long>();

        foreach (var variant in data.Products.SelectMany(p => p.Variants))
        {
            context.ThrowIfStopRequested();

            var link = referenceConverter.GetContentLink(variant.Code, CatalogContentType.CatalogEntry);
            if (ContentReference.IsNullOrEmpty(link))
            {
                continue;
            }

            ids.AddRange(priceDetailService.List(link).Select(p => p.PriceValueId));
        }

        if (ids.Count == 0)
        {
            context.Report("No seeded prices present.");
            return SeedOutcome.Empty;
        }

        priceDetailService.Delete(ids);
        context.Report($"Removed {ids.Count} price(s).");
        return SeedOutcome.Empty.WithRemoved(ids.Count);
    }
}

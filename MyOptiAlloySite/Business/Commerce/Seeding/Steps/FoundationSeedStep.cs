using System.Globalization;
using Mediachase.Commerce;
using Mediachase.Commerce.Inventory;
using Mediachase.Commerce.Markets;
using MyOptiAlloySite.Business.Commerce.Seeding.Data;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Market, currency and warehouse. Everything downstream depends on this: prices cannot
/// be written without a market, and inventory cannot be written without a warehouse.
/// </summary>
public sealed class FoundationSeedStep(
    IMarketService marketService,
    IWarehouseRepository warehouseRepository,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 100;

    public string Name => "Market, currency and warehouse";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();

        var outcome = SeedMarket(context, data.Market);
        context.ThrowIfStopRequested();
        return outcome.Add(SeedWarehouse(context, data.Warehouse));
    }

    private SeedOutcome SeedMarket(SeedRunContext context, SeedMarket seed)
    {
        var currency = new Currency(seed.Currency);
        var language = CultureInfo.GetCultureInfo(seed.Language);
        var existing = marketService.GetMarket(MarketId.Default);

        if (existing is null)
        {
            var market = new MarketImpl(MarketId.Default)
            {
                MarketName = seed.Name,
                IsEnabled = true,
                DefaultCurrency = currency,
                DefaultLanguage = language,
                PricesIncludeTax = false,
            };

            market.CurrenciesCollection.Add(currency);
            market.LanguagesCollection.Add(language);
            foreach (var country in seed.Countries)
            {
                market.CountriesCollection.Add(country);
            }

            marketService.CreateMarket(market);
            context.Report($"Created market '{MarketId.Default.Value}' ({seed.Currency}/{seed.Language}).");
            return SeedOutcome.Empty.WithCreated();
        }

        var updated = new MarketImpl(existing)
        {
            MarketName = seed.Name,
            IsEnabled = true,
            DefaultCurrency = currency,
            DefaultLanguage = language,
        };

        var changed = existing.MarketName != seed.Name
            || !existing.IsEnabled
            || existing.DefaultCurrency != currency
            || !Equals(existing.DefaultLanguage, language);

        if (!updated.CurrenciesCollection.Contains(currency))
        {
            updated.CurrenciesCollection.Add(currency);
            changed = true;
        }

        if (!updated.LanguagesCollection.Contains(language))
        {
            updated.LanguagesCollection.Add(language);
            changed = true;
        }

        foreach (var country in seed.Countries.Where(c => !updated.CountriesCollection.Contains(c)))
        {
            updated.CountriesCollection.Add(country);
            changed = true;
        }

        if (!changed)
        {
            context.Report($"Market '{MarketId.Default.Value}' already up to date.");
            return SeedOutcome.Empty.WithUnchanged();
        }

        marketService.UpdateMarket(updated);
        context.Report($"Updated market '{MarketId.Default.Value}'.");
        return SeedOutcome.Empty.WithUpdated();
    }

    private SeedOutcome SeedWarehouse(SeedRunContext context, SeedWarehouse seed)
    {
        var existing = warehouseRepository.Get(seed.Code);

        if (existing is null)
        {
            warehouseRepository.Save(new Warehouse
            {
                Code = seed.Code,
                Name = seed.Name,
                IsActive = true,
                IsPrimary = true,
                IsFulfillmentCenter = true,
                IsDeliveryLocation = true,
                IsPickupLocation = false,
                SortOrder = 1,
                // The repository rejects a warehouse without contact information.
                ContactInformation = new WarehouseContactInformation
                {
                    Organization = seed.Name,
                    Line1 = "1 Alloy Way",
                    City = "Stockholm",
                    PostalCode = "111 22",
                    CountryCode = "SWE",
                    CountryName = "Sweden",
                    Email = "warehouse@alloy.example",
                    DaytimePhoneNumber = "+46 8 000 000",
                },
            });

            context.Report($"Created warehouse '{seed.Code}'.");
            return SeedOutcome.Empty.WithCreated();
        }

        if (existing.Name == seed.Name && existing.IsActive)
        {
            context.Report($"Warehouse '{seed.Code}' already up to date.");
            return SeedOutcome.Empty.WithUnchanged();
        }

        var updated = new Warehouse(existing) { Name = seed.Name, IsActive = true };
        warehouseRepository.Save(updated);
        context.Report($"Updated warehouse '{seed.Code}'.");
        return SeedOutcome.Empty.WithUpdated();
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var warehouse = warehouseRepository.Get(data.Warehouse.Code);

        if (warehouse?.WarehouseId is null)
        {
            context.Report($"Warehouse '{data.Warehouse.Code}' not present.");
            return SeedOutcome.Empty;
        }

        warehouseRepository.Delete(warehouse.WarehouseId.Value);
        context.Report($"Removed warehouse '{data.Warehouse.Code}'.");

        // The default market is left alone deliberately: Commerce needs one to function,
        // and it is not exclusively ours to delete.
        context.Report($"Left market '{MarketId.Default.Value}' in place.");
        return SeedOutcome.Empty.WithRemoved();
    }
}

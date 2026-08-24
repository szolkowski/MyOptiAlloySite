using Mediachase.Commerce.InventoryService;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Stock levels per variant in the seeded warehouse. Variants with no stock value in the
/// dataset are recorded as untracked rather than as zero stock — the two mean different
/// things in the UI.
/// </summary>
public sealed class InventorySeedStep(
    IInventoryService inventoryService,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 500;

    public string Name => "Inventory";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var warehouseCode = data.Warehouse.Code;
        var outcome = SeedOutcome.Empty;
        var toSave = new List<InventoryRecord>();

        foreach (var variant in data.Products.SelectMany(p => p.Variants))
        {
            context.ThrowIfStopRequested();

            var tracked = variant.Stock.HasValue;
            var quantity = variant.Stock ?? 0m;
            var existing = inventoryService.Get(variant.Code, warehouseCode);

            if (existing is not null)
            {
                if (existing.IsTracked == tracked && existing.PurchaseAvailableQuantity == quantity)
                {
                    outcome = outcome.WithUnchanged();
                    continue;
                }

                var updated = existing.CreateWritableClone();
                updated.IsTracked = tracked;
                updated.PurchaseAvailableQuantity = quantity;
                toSave.Add(updated);
                outcome = outcome.WithUpdated();
                continue;
            }

            toSave.Add(new InventoryRecord
            {
                CatalogEntryCode = variant.Code,
                WarehouseCode = warehouseCode,
                IsTracked = tracked,
                PurchaseAvailableQuantity = quantity,
                PurchaseAvailableUtc = DateTime.UtcNow.Date,
            });
            outcome = outcome.WithCreated();
        }

        if (toSave.Count > 0)
        {
            inventoryService.Save(toSave);
            context.Report($"Saved {toSave.Count} inventory record(s) in warehouse '{warehouseCode}'.");
        }
        else
        {
            context.Report("All inventory already up to date.");
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var codes = data.Products
            .SelectMany(p => p.Variants)
            .Select(v => v.Code)
            .Where(code => inventoryService.Get(code, data.Warehouse.Code) is not null)
            .ToList();

        if (codes.Count == 0)
        {
            context.Report("No seeded inventory present.");
            return SeedOutcome.Empty;
        }

        inventoryService.DeleteByEntry(codes);
        context.Report($"Removed {codes.Count} inventory record(s).");
        return SeedOutcome.Empty.WithRemoved(codes.Count);
    }
}

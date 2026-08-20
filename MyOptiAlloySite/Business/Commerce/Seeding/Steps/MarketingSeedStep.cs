using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.DataAccess;
using EPiServer.Security;
using Mediachase.Commerce;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// A sales campaign with tiered order-level discounts, so the Marketing UI has something
/// real to show and the promotion engine has something to apply.
/// </summary>
public sealed class MarketingSeedStep(
    IContentRepository contentRepository,
    IContentLoader contentLoader,
    SeedDataProvider dataProvider) : ISeedStep
{
    public int Order => 800;

    public string Name => "Campaign and promotions";

    public SeedOutcome Execute(SeedRunContext context)
    {
        var data = dataProvider.Load();
        if (string.IsNullOrWhiteSpace(data.Campaign.Name))
        {
            return SeedOutcome.Empty;
        }

        var currency = new Currency(data.Market.Currency);
        var (campaignLink, outcome) = EnsureCampaign(context, data.Campaign);

        foreach (var promotion in data.Campaign.Promotions)
        {
            context.ThrowIfStopRequested();
            outcome = outcome.Add(EnsurePromotion(context, campaignLink, promotion, currency));
        }

        return outcome;
    }

    public SeedOutcome Remove(SeedRunContext context)
    {
        var data = dataProvider.Load();
        var existing = FindCampaign(data.Campaign.Name);

        if (existing is null)
        {
            context.Report($"Campaign '{data.Campaign.Name}' not present.");
            return SeedOutcome.Empty;
        }

        // Promotions live under the campaign, so this takes them too.
        contentRepository.Delete(existing.ContentLink, forceDelete: true, AccessLevel.NoAccess);
        context.Report($"Removed campaign '{data.Campaign.Name}' and its promotions.");
        return SeedOutcome.Empty.WithRemoved();
    }

    private SalesCampaign FindCampaign(string name) =>
        contentLoader.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot)
            .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private (ContentReference CampaignLink, SeedOutcome Outcome) EnsureCampaign(
        SeedRunContext context, Data.SeedCampaign seed)
    {
        var existing = FindCampaign(seed.Name);
        if (existing is not null)
        {
            context.Report($"Campaign '{seed.Name}' already exists.");
            return (existing.ContentLink, SeedOutcome.Empty.WithUnchanged());
        }

        var campaign = contentRepository.GetDefault<SalesCampaign>(SalesCampaignFolder.CampaignRoot);
        campaign.Name = seed.Name;
        campaign.Description = seed.Description;
        campaign.IsActive = true;
        campaign.ValidFrom = DateTime.UtcNow.Date;
        campaign.ValidUntil = DateTime.UtcNow.Date.AddYears(1);

        var link = contentRepository.Save(campaign, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created campaign '{seed.Name}'.");
        return (link, SeedOutcome.Empty.WithCreated());
    }

    private SeedOutcome EnsurePromotion(
        SeedRunContext context, ContentReference campaignLink, Data.SeedPromotion seed, Currency currency)
    {
        var existing = contentLoader.GetChildren<SpendAmountGetOrderDiscount>(campaignLink)
            .FirstOrDefault(p => string.Equals(p.Name, seed.Name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return SeedOutcome.Empty.WithUnchanged();
        }

        var promotion = contentRepository.GetDefault<SpendAmountGetOrderDiscount>(campaignLink);
        promotion.Name = seed.Name;
        promotion.IsActive = true;
        promotion.Condition = new PurchaseAmount { Amounts = [new Money(seed.SpendThreshold, currency)] };
        promotion.Discount = new MonetaryReward { Percentage = seed.PercentOff, UseAmounts = false };

        contentRepository.Save(promotion, SaveAction.Publish, AccessLevel.NoAccess);
        context.Report($"Created promotion '{seed.Name}'.");
        return SeedOutcome.Empty.WithCreated();
    }
}

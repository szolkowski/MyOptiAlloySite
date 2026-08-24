using System.ComponentModel.DataAnnotations;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.DataAnnotations;

namespace MyOptiAlloySite.Models.Catalog;

/// <summary>
/// A purchasable SKU of an <see cref="AlloyProduct"/>, identified by licence tier
/// and billing period.
/// </summary>
[CatalogContentType(
    GUID = "173F8140-A4B5-4FC8-9D3D-5A59C23B4568",
    MetaClassName = "AlloyVariant",
    GroupName = Globals.GroupNames.Catalog,
    DisplayName = "Alloy variant",
    Description = "A purchasable licence of an Alloy product.")]
public class AlloyVariant : VariationContent
{
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [CultureSpecific]
    public virtual string LicenceTier { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 200)]
    [CultureSpecific]
    public virtual string BillingPeriod { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 300)]
    public virtual int SeatCount { get; set; }
}

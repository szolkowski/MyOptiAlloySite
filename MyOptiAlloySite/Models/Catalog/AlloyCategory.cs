using System.ComponentModel.DataAnnotations;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Web;

namespace MyOptiAlloySite.Models.Catalog;

/// <summary>
/// A grouping of products within a catalog, such as Software or Training.
/// </summary>
[CatalogContentType(
    GUID = "BB6EB2A4-A4F8-45AA-95CE-888D08892E8F",
    MetaClassName = "AlloyCategory",
    GroupName = Globals.GroupNames.Catalog,
    DisplayName = "Alloy category",
    Description = "A category of Alloy products.")]
public class AlloyCategory : NodeContent
{
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [CultureSpecific]
    [UIHint(UIHint.Textarea)]
    public virtual string TeaserText { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 200)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference CategoryImage { get; set; }
}

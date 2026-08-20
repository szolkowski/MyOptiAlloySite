using System.ComponentModel.DataAnnotations;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Web;

namespace MyOptiAlloySite.Models.Catalog;

/// <summary>
/// A sellable Alloy product. The purchasable SKUs are its variants.
/// </summary>
[CatalogContentType(
    GUID = "8363647B-2C91-40A2-965D-494C6312FB24",
    MetaClassName = "AlloyProduct",
    GroupName = Globals.GroupNames.Catalog,
    DisplayName = "Alloy product",
    Description = "An Alloy product with one or more purchasable variants.")]
public class AlloyProduct : ProductContent
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
    [CultureSpecific]
    public virtual XhtmlString Description { get; set; }

    /// <remarks>
    /// ProductPage models this as IList&lt;string&gt;, but catalog content is backed by
    /// Commerce meta-fields, which have no collection type — so this is one point per
    /// line instead. Use <see cref="SellingPoints"/> rather than parsing the raw value.
    /// </remarks>
    [Display(
        Name = "Unique selling points",
        Description = "One selling point per line.",
        GroupName = SystemTabNames.Content,
        Order = 300)]
    [UIHint(UIHint.Textarea)]
    [CultureSpecific]
    public virtual string UniqueSellingPoints { get; set; }

    /// <summary>
    /// <see cref="UniqueSellingPoints"/> split into individual points.
    /// </summary>
    [Ignore]
    public IEnumerable<string> SellingPoints =>
        (UniqueSellingPoints ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public class CatalogType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = default!;

    public CatalogType() { }
    public CatalogType(string type) => Type = type;
}

using NetTopologySuite.Geometries;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.API;

public class Restaurant
{
    public Guid RestaurantId { get; set; }          // PK
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string AdminEmail { get; set; } = string.Empty;

    // PostGIS geography(Point,4326). X=Longitude, Y=Latitude
    public Point? Location { get; set; }            // SRID=4326
    public RestaurantStatus Status { get; set; } = RestaurantStatus.Active;
    // Navigation
    public ICollection<CatalogItem> Items { get; set; } = new List<CatalogItem>();
}

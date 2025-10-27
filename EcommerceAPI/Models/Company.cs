using System.Text.Json.Serialization;

namespace EcommerceAPI.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    [JsonIgnore] // <- evita Company.Products -> Product.Company -> ...
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
namespace EcommerceAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;
}
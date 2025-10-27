namespace EcommerceAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "Cliente";
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
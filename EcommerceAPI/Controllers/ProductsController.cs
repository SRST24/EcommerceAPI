using System.Security.Claims;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    public record ProductCreateDto(string Name, string? Description, decimal Price, int Stock);
    public record ProductUpdateDto(string Name, string? Description, decimal Price, int Stock);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll([FromQuery] int? companyId = null)
    {
        var q = _db.Products.AsNoTracking().Include(p => p.Company).AsQueryable();
        if (companyId.HasValue) q = q.Where(p => p.CompanyId == companyId.Value);
        var list = await q.ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var p = await _db.Products.Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Empresa")]
    public async Task<ActionResult> Create(ProductCreateDto dto)
    {
        var userCompanyId = User.FindFirst("companyId")?.Value;
        if (string.IsNullOrWhiteSpace(userCompanyId)) return Forbid("Usuario Empresa sin CompanyId.");

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            CompanyId = int.Parse(userCompanyId)
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Empresa")]
    public async Task<ActionResult> Update(int id, ProductUpdateDto dto)
    {
        var userCompanyId = int.Parse(User.FindFirst("companyId")?.Value ?? "0");
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        if (p.CompanyId != userCompanyId) return Forbid("No puedes editar productos de otra empresa.");

        p.Name = dto.Name.Trim();
        p.Description = dto.Description;
        p.Price = dto.Price;
        p.Stock = dto.Stock;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Empresa")]
    public async Task<ActionResult> Delete(int id)
    {
        var userCompanyId = int.Parse(User.FindFirst("companyId")?.Value ?? "0");
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        if (p.CompanyId != userCompanyId) return Forbid("No puedes borrar productos de otra empresa.");

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

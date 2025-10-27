using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("companies")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CompaniesController(AppDbContext db) => _db = db;

    public record CompanyCreateDto(string Name);
    public record CompanyUpdateDto(string Name);
    public record CreateCompanyUserDto(string Email, string Password);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Company>>> GetAll()
    {
        var list = await _db.Companies.AsNoTracking().ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Company>> GetById(int id)
    {
        var c = await _db.Companies.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(CompanyCreateDto dto)
    {
        var c = new Company { Name = dto.Name.Trim() };
        _db.Companies.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, CompanyUpdateDto dto)
    {
        var c = await _db.Companies.FindAsync(id);
        if (c is null) return NotFound();
        c.Name = dto.Name.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var c = await _db.Companies.FindAsync(id);
        if (c is null) return NotFound();
        _db.Companies.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Crear usuario con rol Empresa asignado a una Company
    [HttpPost("{companyId:int}/users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateCompanyUser(int companyId, CreateCompanyUserDto dto)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            return NotFound("Company no existe.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email ya registrado.");

        var user = new User
        {
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Empresa",
            CompanyId = companyId
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Usuario Empresa creado.", userId = user.Id });
    }
}

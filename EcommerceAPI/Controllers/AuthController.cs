using System.Security.Claims;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _token;

    public AuthController(AppDbContext db, TokenService token)
    {
        _db = db;
        _token = token;
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);

    // Registro de Cliente
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest("El email ya está registrado.");

        var user = new User
        {
            Email = req.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "Cliente"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registrado como Cliente." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Credenciales inválidas.");

        var token = _token.CreateToken(user);
        return Ok(new { token, role = user.Role, companyId = user.CompanyId, userId = user.Id });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var companyId = User.FindFirst("companyId")?.Value;

        var exists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!exists) return NotFound();

        return Ok(new { id, email, role, companyId });
    }
}

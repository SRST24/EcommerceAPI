using System.Security.Claims;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("orders")]
[Authorize(Roles = "Cliente")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public record AddItemDto(int ProductId, int Quantity);

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private async Task<Order> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

        if (cart is null)
        {
            cart = new Order { UserId = userId, Status = "Cart" };
            _db.Orders.Add(cart);
            await _db.SaveChangesAsync();
        }
        return cart;
    }

    [HttpGet("cart")]
    public async Task<ActionResult<Order>> GetCart()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var cart = await GetOrCreateCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("cart/items")]
    public async Task<ActionResult> AddItem(AddItemDto dto)
    {
        if (dto.Quantity <= 0) return BadRequest("Cantidad inválida.");
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var cart = await GetOrCreateCartAsync(userId);

        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product is null) return NotFound("Producto no existe.");

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
        if (existing is null)
        {
            cart.Items.Add(new OrderItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = product.Price
            });
        }
        else
        {
            existing.Quantity += dto.Quantity;
        }

        await _db.SaveChangesAsync();
        return Ok(cart);
    }

    [HttpDelete("cart/items/{itemId:int}")]
    public async Task<ActionResult> RemoveItem(int itemId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return NotFound();

        _db.OrderItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("cart/checkout")]
    public async Task<ActionResult> Checkout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var cart = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

        if (cart is null || !cart.Items.Any())
            return BadRequest("Carrito vacío.");

        // Validar stock
        foreach (var it in cart.Items)
        {
            if (it.Quantity > it.Product.Stock)
                return BadRequest($"Sin stock suficiente para {it.Product.Name}.");
        }

        // Descontar stock
        foreach (var it in cart.Items)
        {
            it.Product.Stock -= it.Quantity;
        }

        cart.Status = "Placed";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Pedido confirmado", orderId = cart.Id });
    }
}

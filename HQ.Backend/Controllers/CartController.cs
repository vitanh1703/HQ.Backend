using HQ.Backend.Data;
using HQ.Backend.DTOs;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context) { _context = context; }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] DTOs.AddToCartRequest request)
        {
            var variant = await _context.ProductVariants.FindAsync(request.VariantId);
            if (variant == null) return NotFound(new { message = "Sản phẩm không tồn tại!" });

            if (variant.StockQuantity < request.Quantity)
                return BadRequest(new { message = "Số lượng trong kho không đủ!" });

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == request.UserId);
            if (cart == null)
            {
                cart = new Cart { UserId = request.UserId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.VariantId == request.VariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity
                };
                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã thêm vào giỏ hàng!" });
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cartData = await _context.Carts
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    CartId = c.Id,
                    Items = _context.CartItems
                        .Where(ci => ci.CartId == c.Id)
                        .Join(_context.ProductVariants, ci => ci.VariantId, v => v.Id, (ci, v) => new { ci, v })
                        .Join(_context.Products, joined => joined.v.ProductId, p => p.Id, (joined, p) => new
                        {
                            Id = joined.ci.Id,
                            VariantId = joined.v.Id,
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Size = joined.v.Size,
                            Color = joined.v.Color,
                            Price = joined.v.Price,
                            Quantity = joined.ci.Quantity,
                            Total = joined.v.Price * joined.ci.Quantity,
                            Image = p.ImageUrl
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            return Ok(cartData);
        }

        [HttpGet("checkout/{userId}")]
        public async Task<IActionResult> GetCheckout(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Người dùng không tồn tại!" });

            var cartData = await _context.Carts
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    CartId = c.Id,
                    Items = _context.CartItems
                        .Where(ci => ci.CartId == c.Id)
                        .Join(_context.ProductVariants, ci => ci.VariantId, v => v.Id, (ci, v) => new { ci, v })
                        .Join(_context.Products, joined => joined.v.ProductId, p => p.Id, (joined, p) => new
                        {
                            Id = joined.ci.Id,
                            VariantId = joined.v.Id,
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Size = joined.v.Size,
                            Color = joined.v.Color,
                            Price = joined.v.Price,
                            Quantity = joined.ci.Quantity,
                            Total = joined.v.Price * joined.ci.Quantity,
                            Image = p.ImageUrl
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (cartData == null)
            {
                return Ok(new
                {
                    cartId = 0,
                    user = new
                    {
                        fullName = user.FullName,
                        email = user.Email,
                        phone = user.Phone,
                        address = user.Address
                    },
                    items = new object[] { }
                });
            }

            return Ok(new
            {
                cartId = cartData.CartId,
                user = new
                {
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.Phone,
                    address = user.Address
                },
                items = cartData.Items
            });
        }

        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
                return NotFound(new { message = "Sản phẩm trong giỏ hàng không tồn tại!" });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa sản phẩm khỏi giỏ hàng thành công!" });
        }
    }
}

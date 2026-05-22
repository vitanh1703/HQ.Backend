using HQ.Backend.Data;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PromotionsController(AppDbContext context)
        {
            _context = context;
        }

        // [HttpGet]
        // public async Task<IActionResult> GetPromotions()
        // {
        //     var promotions = await _context.Promotions
        //         .Where(p => p.Status == 1 && p.EndDate >= DateTime.Now) // Dùng Status thay cho Active
        //         .Select(p => new {
        //             p.Id,
        //             p.Code,
        //             p.Description,
        //             p.DiscountValue,
        //             p.DiscountType,
        //             DiscountText = p.DiscountType == "Percentage" ? p.DiscountValue + "%" : p.DiscountValue + "đ",
        //             StartDate = p.StartDate.ToString("dd/MM/yyyy"),
        //             EndDate = p.EndDate.ToString("dd/MM/yyyy")
        //         })
        //         .OrderByDescending(p => p.Id)
        //         .ToListAsync();

        //     return Ok(promotions);
        // }
        [HttpGet]
        public async Task<IActionResult> GetPromotions()
        {
            // Bỏ filter Status == 1 và EndDate để Admin thấy được tất cả mã đã hết hạn hoặc bị dừng
            var promotions = await _context.Promotions
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(promotions);
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidatePromotion(string code)
        {
            var promotion = await _context.Promotions
                .Where(p => p.Status == 1 && p.EndDate >= DateTime.Now && p.Code == code)
                .Select(p => new {
                    p.Code,
                    p.Description,
                    p.DiscountValue,
                    p.DiscountType
                })
                .FirstOrDefaultAsync();

            if (promotion == null)
            {
                return NotFound(new { message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            return Ok(promotion);
        }
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] Promotion promotion)
        {
            if (promotion == null) return BadRequest();

            try
            {
                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();
                return Ok(promotion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {
            if (id != promotion.Id) return BadRequest("ID không khớp.");

            // Đánh dấu bản ghi này đã bị thay đổi
            _context.Entry(promotion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thành công!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Promotions.Any(p => p.Id == id)) return NotFound();
                throw;
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa khuyến mãi thành công." });
        }
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try 
            {
                var inventory = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.Category)
                    .Select(v => new {
                        sku = v.Sku,
                        productName = v.Product.Name,
                        categoryName = v.Product.Category != null ? v.Product.Category.Name : "N/A",
                        stockQuantity = v.StockQuantity,
                        size = v.Size,
                        color = v.Color,
                        // FIX: So sánh Status không phân biệt hoa thường (Pending/pending)
                        reservedQuantity = _context.OrderItems
                            .Where(oi => oi.VariantId == v.Id && 
                                        oi.Order.Status.ToLower() == "pending") 
                            .Sum(oi => (int?)oi.Quantity) ?? 0,
                        updatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToListAsync();

                return Ok(inventory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}
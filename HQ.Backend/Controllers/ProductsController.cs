using HQ.Backend.Data;
using Microsoft.AspNetCore.Http;
using HQ.Backend.Models;
using HQ.Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int? category)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (category.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == category.Value);
            }

            var products = await productsQuery
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.BrandText,
                    p.ImageUrl,
                    p.Description,
                    p.CategoryId,
                    Variants = _context.ProductVariants
                        .Where(v => v.ProductId == p.Id)
                        .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.BrandText,
                    p.ImageUrl,
                    p.Description,
                    p.CategoryId,
                    Variants = _context.ProductVariants
                        .Where(v => v.ProductId == p.Id)
                        .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new {
                    c.Id,
                    c.Name,
                    c.Description
                })
                .ToListAsync();

            return Ok(categories);
        }

          // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants) // ✅ bắt buộc Include
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.BrandText,
                    p.Description,
                    p.ImageUrl,
                    Variants = p.Variants.Select(v => new
                    {
                        v.Id,
                        v.Size,
                        v.Color,
                        v.Price,
                        v.StockQuantity,
                        v.Sku
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "Sản phẩm không tồn tại" });

            return Ok(product);
        }

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetProductReviews(int id)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == id);
            if (!productExists)
                return NotFound(new { message = "Sản phẩm không tồn tại" });

            var reviews = await (
                from review in _context.Reviews
                join user in _context.Users on review.UserId equals user.Id
                where review.ProductId == id
                orderby review.CreatedAt descending
                select new
                {
                    review.Id,
                    review.UserId,
                    review.ProductId,
                    rating = review.Rating ?? 0,
                    comment = review.Comment,
                    createdAt = review.CreatedAt,
                    userName = user.FullName
                }
            ).ToListAsync();

            var averageRating = reviews.Count == 0
                ? 0
                : Math.Round(reviews.Average(r => r.rating), 1);

            return Ok(new
            {
                averageRating,
                totalReviews = reviews.Count,
                reviews
            });
        }

        // ===== ADMIN ENDPOINTS =====

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllProductsForAdmin()
        {
            var products = await _context.Products
                .Include(p => p.Variants)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.BrandText,
                    p.Description,
                    p.ImageUrl,
                    p.CategoryId,
                    p.SupplierId,
                    Variants = p.Variants.Select(v => new
                    {
                        v.Id,
                        v.Size,
                        v.Color,
                        v.Price,
                        v.StockQuantity,
                        v.Sku
                    }).ToList()
                })
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost("admin/create")]
        public async Task<IActionResult> CreateProductAdmin([FromBody] CreateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });

            try
            {
                var product = new Product
                {
                    Name = request.Name.Trim(),
                    BrandText = request.BrandText?.Trim() ?? "H&Q",
                    Description = request.Description?.Trim(),
                    ImageUrl = request.ImageUrl?.Trim(),
                    CategoryId = request.CategoryId > 0 ? request.CategoryId : null,
                    SupplierId = request.SupplierId > 0 ? request.SupplierId : null
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Add variants if provided
                if (request.Variants != null && request.Variants.Any())
                {
                    var variantList = new List<ProductVariant>();
                    foreach (var (variant, index) in request.Variants.Select((v, i) => (v, i)))
                    {
                        var sku = !string.IsNullOrWhiteSpace(variant.Sku) 
                            ? variant.Sku.Trim() 
                            : $"V{product.Id}{index + 1:D3}";

                        if (sku.Length > 50)
                            sku = sku.Substring(0, 50);

                        variantList.Add(new ProductVariant
                        {
                            ProductId = product.Id,
                            Size = (variant.Size?.Trim() ?? "").Substring(0, Math.Min(10, (variant.Size?.Trim() ?? "").Length)),
                            Color = (variant.Color?.Trim() ?? "").Substring(0, Math.Min(30, (variant.Color?.Trim() ?? "").Length)),
                            Price = variant.Price,
                            StockQuantity = Math.Max(0, variant.StockQuantity),
                            Sku = sku
                        });
                    }

                    _context.ProductVariants.AddRange(variantList);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { 
                    id = product.Id,
                    name = product.Name,
                    variantCount = request.Variants?.Count ?? 0,
                    message = "Sản phẩm được tạo thành công" 
                });
            }
            catch (DbUpdateException ex)
            {
                var innerError = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { 
                    message = "Lỗi khi lưu dữ liệu vào cơ sở dữ liệu",
                    error = innerError
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    message = "Lỗi khi tạo sản phẩm",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("admin/{id}")]
        public async Task<IActionResult> UpdateProductAdmin(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return NotFound(new { message = "Sản phẩm không tồn tại" });

                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });

                product.Name = request.Name.Trim();
                product.BrandText = request.BrandText?.Trim() ?? product.BrandText;
                product.Description = request.Description?.Trim();
                product.ImageUrl = request.ImageUrl?.Trim();
                product.CategoryId = request.CategoryId ?? product.CategoryId;
                product.SupplierId = request.SupplierId ?? product.SupplierId;

                // Xóa variants cũ nếu có variants mới
                if (request.Variants != null && request.Variants.Count > 0)
                {
                    var existingVariants = product.Variants.ToList();
                    var incomingVariants = request.Variants;

                    // 1. Cập nhật các biến thể cũ hoặc thêm mới
                    foreach (var v in incomingVariants)
                    {
                        var existing = existingVariants.FirstOrDefault(ev => 
                            ev.Size == v.Size?.Trim() && ev.Color == v.Color?.Trim());

                        if (existing != null)
                        {
                            // Cập nhật thông tin, giữ nguyên ID để tránh lỗi Foreign Key
                            existing.Price = v.Price > 0 ? v.Price : 0;
                            existing.StockQuantity = v.StockQuantity;
                            existing.Sku = v.Sku?.Trim() ?? existing.Sku;
                            existingVariants.Remove(existing); // Đánh dấu đã xử lý
                        }
                        else
                        {
                            // Thêm mới nếu chưa có
                            _context.ProductVariants.Add(new ProductVariant
                            {
                                ProductId = product.Id,
                                Size = v.Size?.Trim() ?? "",
                                Color = v.Color?.Trim() ?? "",
                                Price = v.Price > 0 ? v.Price : 0,
                                StockQuantity = v.StockQuantity,
                                Sku = v.Sku?.Trim()
                            });
                        }
                    }

                    // 2. Xóa những biến thể không còn trong danh sách gửi lên
                    foreach (var oldVariant in existingVariants)
                    {
                        // Kiểm tra xem biến thể này đã có đơn hàng chưa
                        var isUsed = await _context.OrderItems.AnyAsync(oi => oi.VariantId == oldVariant.Id);
                        if (isUsed)
                        {
                            return BadRequest(new { message = $"Không thể xóa biến thể {oldVariant.Size}/{oldVariant.Color} vì đã có đơn hàng liên quan. Hãy chỉ cập nhật thông tin." });
                        }
                        _context.ProductVariants.Remove(oldVariant);
                    }
                }

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    id = product.Id,
                    name = product.Name,
                    message = "Sản phẩm được cập nhật thành công"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Lỗi cơ sở dữ liệu: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteProductAdmin(int id)
        {
            var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Sản phẩm không tồn tại" });

            // Kiểm tra xem bất kỳ biến thể nào của sản phẩm đã được đặt hàng chưa trước khi xóa toàn bộ
            foreach (var variant in product.Variants)
            {
                if (await _context.OrderItems.AnyAsync(oi => oi.VariantId == variant.Id))
                {
                    return BadRequest(new { message = "Không thể xóa sản phẩm này vì đã có đơn hàng liên quan trong hệ thống." });
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sản phẩm được xóa thành công" });
        }
    }
}

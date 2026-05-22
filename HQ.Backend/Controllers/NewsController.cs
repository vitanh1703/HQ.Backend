using HQ.Backend.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQ.Backend.Models;
using HQ.Backend.DTOs;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NewsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var news = await _context.News
                .Select(n => new {
                    n.Id,
                    n.Category,
                    n.Title,
                    Date = n.PublishDate.ToString("dd/MM"),
                    Img = n.ImgUrl,
                    Desc = n.Description
                })
                .ToListAsync();

            return Ok(news);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetNewsCategories()
        {
            var categories = await _context.News
                .Select(n => n.Category)
                .Distinct()
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("titles")]
        public async Task<IActionResult> GetNewsTitles()
        {
            var titles = await _context.News
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Category
                })
                .ToListAsync();

            return Ok(titles);
        }

        // GET: api/news/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNewsById(int id)
        {
            var news = await _context.News
                .Where(n => n.Id == id)
                .Select(n => new {
                    n.Id,
                    n.Category,
                    n.Title,
                    Date = n.PublishDate.ToString("dd/MM/yyyy"),
                    Img = n.ImgUrl,
                    Desc = n.Description,
                    n.Content // Quan trọng: Lấy thêm nội dung chi tiết
                })
                .FirstOrDefaultAsync();

            if (news == null)
                return NotFound(new { message = "Không tìm thấy tin tức" });

            return Ok(news);
        }

        // ===== ADMIN ENDPOINTS =====

        [HttpPost("admin/create")]
        public async Task<IActionResult> CreateNews([FromBody] CreateNewsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Tiêu đề tin tức là bắt buộc" });

            try
            {
                var news = new News
                {
                    Category = request.Category?.Trim() ?? "Tin tức",
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim(),
                    Content = request.Content?.Trim(),
                    ImgUrl = request.ImgUrl?.Trim(),
                    PublishDate = request.PublishDate ?? DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.News.Add(news);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    id = news.Id,
                    title = news.Title,
                    message = "Tạo tin tức thành công" 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    message = "Lỗi khi tạo tin tức",
                    error = ex.Message
                });
            }
        }

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllNewsForAdmin()
        {
            var news = await _context.News
                .Select(n => new
                {
                    n.Id,
                    n.Category,
                    n.Title,
                    n.Description,
                    n.Content,
                    n.ImgUrl,
                    n.PublishDate,
                    n.CreatedAt
                })
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();

            return Ok(news);
        }

        [HttpPut("admin/{id}")]
        public async Task<IActionResult> UpdateNews(int id, [FromBody] UpdateNewsRequest request)
        {
            try
            {
                var news = await _context.News.FindAsync(id);
                if (news == null)
                    return NotFound(new { message = "Tin tức không tồn tại" });

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { message = "Tiêu đề tin tức là bắt buộc" });

                news.Category = request.Category?.Trim() ?? news.Category;
                news.Title = request.Title.Trim();
                news.Description = request.Description?.Trim();
                news.Content = request.Content?.Trim();
                news.ImgUrl = request.ImgUrl?.Trim();
                news.PublishDate = request.PublishDate ?? news.PublishDate;

                _context.News.Update(news);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    id = news.Id,
                    title = news.Title,
                    message = "Cập nhật tin tức thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
                return NotFound(new { message = "Tin tức không tồn tại" });

            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa tin tức thành công" });
        }
    }
}
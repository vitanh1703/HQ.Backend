using HQ.Backend.Data;
using Microsoft.AspNetCore.Http;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new {
                        r.Id,
                        r.UserId,
                        r.ProductId,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        userName = u.FullName
                    })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new {
                        r.Id,
                        r.UserId,
                        r.ProductId,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        userName = u.FullName
                    })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("rating/{rating}")]
        public async Task<IActionResult> GetByRating(int rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            var reviews = await _context.Reviews
                .Where(r => r.Rating >= rating)
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new {
                        r.Id,
                        r.UserId,
                        r.ProductId,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        userName = u.FullName
                    })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            var review = new Review
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByProduct), new { productId = review.ProductId }, review);
        }
    }

    public class CreateReviewRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}

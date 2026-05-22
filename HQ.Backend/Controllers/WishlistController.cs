using HQ.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        private DbParameter CreateParameter(DbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            return param;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetWishlist(int userId)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT variant_id FROM wishlist WHERE user_id = @userId";
                cmd.Parameters.Add(CreateParameter(cmd, "@userId", userId));
                
                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<int>();
                while (await reader.ReadAsync())
                {
                    list.Add(reader.GetInt32(0));
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách yêu thích", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] WishlistRequest request)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM wishlist WHERE user_id = @userId AND variant_id = @variantId";
                checkCmd.Parameters.Add(CreateParameter(checkCmd, "@userId", request.UserId));
                checkCmd.Parameters.Add(CreateParameter(checkCmd, "@variantId", request.VariantId));
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                
                if (count > 0)
                    return BadRequest(new { message = "Sản phẩm đã có trong danh sách yêu thích" });

                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO wishlist (user_id, variant_id, added_at) VALUES (@userId, @variantId, NOW())";
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@userId", request.UserId));
                insertCmd.Parameters.Add(CreateParameter(insertCmd, "@variantId", request.VariantId));
                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Đã thêm vào danh sách yêu thích" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm danh sách yêu thích", error = ex.Message });
            }
        }

        [HttpDelete("{userId}/{variantId}")]
        public async Task<IActionResult> RemoveFromWishlist(int userId, int variantId)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var deleteCmd = conn.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM wishlist WHERE user_id = @userId AND variant_id = @variantId";
                deleteCmd.Parameters.Add(CreateParameter(deleteCmd, "@userId", userId));
                deleteCmd.Parameters.Add(CreateParameter(deleteCmd, "@variantId", variantId));
                var rowsAffected = await deleteCmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                    return NotFound(new { message = "Không tìm thấy sản phẩm trong danh sách yêu thích" });

                return Ok(new { message = "Đã xóa khỏi danh sách yêu thích" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa khỏi danh sách yêu thích", error = ex.Message });
            }
        }
    }

    public class WishlistRequest
    {
        public int UserId { get; set; }
        public int VariantId { get; set; }
    }
}
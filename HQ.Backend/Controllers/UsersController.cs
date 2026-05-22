using HQ.Backend.Data;
using HQ.Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPut("{id}/info")]
        public async Task<IActionResult> UpdateInfo(int id, [FromBody] UpdateInfoRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) 
                {
                    return NotFound(new { message = "Không tìm thấy người dùng" });
                }

                user.FullName = request.FullName;
                user.Phone = request.Phone;
                user.Address = request.Address;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật thông tin thành công", user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông tin", error = ex.Message });
            }
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) 
                {
                    return NotFound(new { message = "Không tìm thấy người dùng" });
                }

                if (string.IsNullOrEmpty(user.Password))
                {
                     return BadRequest(new { message = "Tài khoản liên kết Google không thể đổi mật khẩu." });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đổi mật khẩu", error = ex.Message });
            }
        }
        // 1. Thêm hàm lấy danh sách User
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var usersList = _context.Users
                    .Where(u => u.Role == "Customer") 
                    .ToList();
                
                // Lấy toàn bộ đơn hàng để map trạng thái
                var ordersList = _context.Orders.ToList();

                var result = usersList.Select(u => new {
                    id = u.Id,
                    fullName = u.FullName,
                    email = u.Email,
                    phone = u.Phone,
                    address = u.Address,
                    status = u.Status,
                    createdAt = u.CreatedAt,
                    // Lấy trạng thái đơn hàng mới nhất
                    latestOrderStatus = ordersList
                        .Where(o => o.UserId == u.Id)
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => o.Status)
                        .FirstOrDefault()
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách", error = ex.Message });
            }
        }

        // 2. Thêm hàm xóa User (Xóa ở cả Database)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Không thể xóa khách hàng này vì có dữ liệu liên quan (đơn hàng...)" });
            }
        }
    }
}
using DocumentFormat.OpenXml.InkML;
using HQ.Backend.Data;
using HQ.Backend.DTOs;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using DocumentFormat.OpenXml.Drawing.Diagrams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo OrderCode dễ đọc (Ví dụ: HQ + Ngày giờ + 3 số ngẫu nhiên)
                string orderCode = "HQ" + DateTime.Now.ToString("yyMMddHHmm") + new Random().Next(100, 999);

                var order = new Order
                {
                    UserId = request.UserId,
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    TotalAmount = request.TotalAmount,
                    OrderCode = orderCode, 
                    Status = "Pending",
                    OrderDate = DateTime.Now,
                    PaymentDate = null
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 2. Lưu chi tiết sản phẩm
                if (request.Items != null && request.Items.Any())
                {
                    var orderItems = request.Items.Select(item => new OrderItem
                    {
                        OrderId = order.Id,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        PriceAtPurchase = item.PriceAtPurchase
                    }).ToList();

                    _context.OrderItems.AddRange(orderItems);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Trả về orderCode để Frontend hiển thị QR
                return Ok(new
                {
                    id = order.Id,
                    orderCode = order.OrderCode
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi tạo đơn", error = ex.Message });
            }
        }


        [HttpPost("webhook/casso")]
        public async Task<IActionResult> CassoWebhook([FromBody] CassoWebhookRequest request)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (request?.data == null || !request.data.Any())
            {
                return BadRequest(new { error = 1, message = "Không có dữ liệu giao dịch" });
            }

            foreach (var transaction in request.data)
            {
                string tranDescription = transaction.description?.ToUpper() ?? "";
                Console.WriteLine($"Đang xử lý giao dịch: {tranDescription} - Số tiền: {transaction.amount}");

                // 2. Tìm đơn hàng 'Pending' có OrderCode nằm trong nội dung chuyển khoản
                // Sử dụng .AsEnumerable() hoặc truy vấn cẩn thận để tránh lỗi dịch SQL nếu chuỗi phức tạp
                var matchedOrder = await _context.Set<Order>()
                    .FirstOrDefaultAsync(o => o.Status == "Pending" &&
                                             tranDescription.Contains(o.OrderCode.ToUpper()));

                if (matchedOrder != null)
                {
                    // 3. CẬP NHẬT TRẠNG THÁI (Đây là bước bạn đang thiếu)
                    matchedOrder.Status = "Success";
                    matchedOrder.OrderDate = DateTime.UtcNow;

                    Console.WriteLine($"Khớp thành công đơn hàng: {matchedOrder.OrderCode}");
                }
            }

            // 4. Lưu tất cả thay đổi vào DB
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { error = 0, message = "Xử lý thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = 1, message = "Lỗi Database: " + ex.Message });
            }
        }

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.OrderCode,
                    o.FullName,
                    o.Email,
                    o.Phone,
                    o.Address,
                    o.TotalAmount,
                    o.Status,
                    o.OrderDate,
                    o.PaymentDate
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderCode,
                    o.TotalAmount,
                    o.Status,
                    o.OrderDate
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                // 1. Cập nhật trạng thái đơn hàng
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE orders SET status = @status WHERE id = @id";

                var pStatus = updateCmd.CreateParameter();
                pStatus.ParameterName = "@status";
                pStatus.Value = request.Status;
                updateCmd.Parameters.Add(pStatus);

                var pId = updateCmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = id;
                updateCmd.Parameters.Add(pId);

                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                    return NotFound(new { message = "Không tìm thấy đơn hàng để cập nhật" });

                // 2. Nếu thanh toán thành công, tự động dọn dẹp giỏ hàng của người dùng
                if (request.Status == "Success")
                {
                    using var getUserIdCmd = conn.CreateCommand();
                    getUserIdCmd.CommandText = "SELECT user_id FROM orders WHERE id = @id";

                    var pId2 = getUserIdCmd.CreateParameter();
                    pId2.ParameterName = "@id";
                    pId2.Value = id;
                    getUserIdCmd.Parameters.Add(pId2);

                    var userIdObj = await getUserIdCmd.ExecuteScalarAsync();

                    if (userIdObj != null && userIdObj != DBNull.Value)
                    {
                        int userId = Convert.ToInt32(userIdObj);

                        using var clearCartCmd = conn.CreateCommand();
                        clearCartCmd.CommandText = "DELETE FROM cart_items WHERE cart_id = (SELECT id FROM carts WHERE user_id = @userId) AND variant_id IN (SELECT variant_id FROM order_items WHERE order_id = @id)";

                        var pUserId = clearCartCmd.CreateParameter();
                        pUserId.ParameterName = "@userId";
                        pUserId.Value = userId;
                        clearCartCmd.Parameters.Add(pUserId);

                        var pOrderId = clearCartCmd.CreateParameter();
                        pOrderId.ParameterName = "@id";
                        pOrderId.Value = id;
                        clearCartCmd.Parameters.Add(pOrderId);

                        await clearCartCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { message = "Cập nhật trạng thái đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái đơn hàng", error = ex.Message });
            }
        }

        [HttpDelete("cleanup-unpaid")]
        public async Task<IActionResult> CleanupUnpaidOrders()
        {
            try
            {
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE orders SET status = 'Cancel' WHERE status = 'Pending' AND order_date <= NOW() - INTERVAL 24 HOUR"
                );

                if (rowsAffected == 0)
                {
                    return Ok(new { message = "Không có đơn hàng nào quá hạn cần xử lý." });
                }

                return Ok(new { message = $"Đã hủy thành công {rowsAffected} đơn hàng chưa thanh toán quá 24h và hoàn trả kho." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xử lý đơn hàng quá hạn", error = ex.Message });
            }
        }

        [HttpDelete("delete-canceled")]
        public async Task<IActionResult> DeleteCanceledOrders()
        {
            try
            {
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM orders WHERE status = 'Cancel'"
                );

                if (rowsAffected == 0)
                {
                    return Ok(new { message = "Không có đơn hàng đã hủy nào cần xóa." });
                }

                return Ok(new { message = $"Đã xóa thành công {rowsAffected} đơn hàng đã hủy khỏi hệ thống." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa đơn hàng đã hủy", error = ex.Message });
            }
        }
    }
}
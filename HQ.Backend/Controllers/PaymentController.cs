using HQ.Backend.DTOs;
using HQ.Backend.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQ.Backend.Data;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-payment")]
        public IActionResult CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào chặn đứng lỗi Null trước khi tính toán
                if (request == null || request.OrderId <= 0 || request.Amount <= 0)
                {
                    return BadRequest(new { message = "Thông tin đơn hàng hoặc số tiền không hợp lệ!" });
                }

                string vnp_Returnurl = "https://vitanh17.id.vn/checkout"; 
                string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string vnp_TmnCode = "0DRVD7D3";
                string vnp_HashSecret = "FOT4EZMW8ZT729XNKBJR3NW7GNTPA6HX";

                VnPayLibrary vnpay = new VnPayLibrary();

                // Chuẩn hóa số tiền thành chuỗi số nguyên thuần túy
                long amountInVnd = (long)Math.Round((decimal)request.Amount);
                long vnpAmount = amountInVnd * 100;

                // Xử lý IP an toàn cho môi trường Linux Container
                string ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }
                if (ipAddress.Contains(","))
                {
                    ipAddress = ipAddress.Split(',')[0].Trim();
                }
                if (ipAddress.Contains("::ffff:"))
                {
                    ipAddress = ipAddress.Replace("::ffff:", "");
                }
                if (ipAddress.Contains(":") || string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "127.0.0.1";
                }

                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString()); 
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + request.OrderId);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", request.OrderId.ToString()); 

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                // In kiểm tra link sinh ra trong log hệ thống để kiểm soát luồng
                Console.WriteLine($"[VNPay Success URL]: {paymentUrl}");

                return Ok(new { url = paymentUrl });
            }
            catch (Exception ex)
            {
                // Ghi nhận log lỗi cụ thể ra màn hình đen Railway 
                Console.WriteLine("[VNPay Create Error Critical]: " + ex.ToString());
                return StatusCode(500, new { message = "Lỗi xử lý khởi tạo cổng thanh toán VNPay!", error = ex.Message });
            }
        }

        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnPayIPN()
        {
            try
            {
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (var (key, value) in vnpayData)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, value.ToString());
                    }
                }

                string secretKey = "FOT4EZMW8ZT729XNKBJR3NW7GNTPA6HX";
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, secretKey);

                if (checkSignature)
                {
                    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                    int orderId = int.Parse(vnpay.GetResponseData("vnp_TxnRef"));

                    if (vnp_ResponseCode == "00") 
                    {
                        var order = await _context.Orders.FindAsync(orderId);
                        if (order != null && order.Status != "Success")
                        {
                            order.Status = "Success"; 
                            await _context.SaveChangesAsync();
                        }
                    }
                    
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }

                return BadRequest(new { RspCode = "97", Message = "Invalid Signature" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VNPay IPN Error]: " + ex.Message);
                return StatusCode(500, new { RspCode = "99", Message = "Internal Error" });
            }
        }
    }
}

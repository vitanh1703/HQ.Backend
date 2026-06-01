using HQ.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HQ.Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class PasswordResetController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry, int FailedAttempts)> _otpStorage = new();

        public PasswordResetController(AppDbContext context)
        {
            _context = context;
        }

        public class ForgotPasswordDto { public string Email { get; set; } }
        public class VerifyOtpDto { public string Email { get; set; } public string Otp { get; set; } }
        public class ResetPasswordDto { public string Email { get; set; } public string Otp { get; set; } public string NewPassword { get; set; } }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null)
                return NotFound(new { message = "Email này không tồn tại trong hệ thống!" });

            if (user.AuthProvider == "google")
                return BadRequest(new { message = "Tài khoản này được đăng nhập bằng Google, không thể đổi mật khẩu qua hệ thống!" });

            string otp = new Random().Next(100000, 999999).ToString();
            // Lưu OTP có thời hạn 5 phút
            _otpStorage[request.Email] = (otp, DateTime.Now.AddMinutes(5), 0);

            bool isSent = await SendEmailAsync(request.Email, "Mã OTP đặt lại mật khẩu - H&Q Store", otp);

            Console.WriteLine($"====== [QUÊN MẬT KHẨU H&Q] OTP CỦA {request.Email} LÀ: {otp} ======");

            if (isSent) 
                return Ok(new { message = "Mã OTP đã được gửi thật qua API Brevo!" });
            
            return StatusCode(500, new { message = "Gửi mail thất bại từ Mail Server Brevo API!" });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpDto request)
        {
            if (!_otpStorage.TryGetValue(request.Email, out var data))
                return BadRequest(new { message = "OTP không tồn tại hoặc đã hết hạn." });

            if (DateTime.Now > data.Expiry)
            {
                _otpStorage.TryRemove(request.Email, out _);
                return BadRequest(new { message = "OTP đã hết hạn." });
            }

            if (data.Otp != request.Otp)
            {
                int failedAttempts = data.FailedAttempts + 1;
                if (failedAttempts >= 5)
                {
                    _otpStorage.TryRemove(request.Email, out _);
                    return BadRequest(new { message = "Bạn đã nhập sai quá 5 lần. Mã OTP đã bị hủy!" });
                }
                _otpStorage[request.Email] = (data.Otp, data.Expiry, failedAttempts);
                return BadRequest(new { message = $"Mã OTP không đúng. Bạn còn {5 - failedAttempts} lần thử." });
            }

            return Ok(new { message = "Xác thực thành công!" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (!_otpStorage.TryGetValue(request.Email, out var data) || data.Otp != request.Otp)
                return BadRequest(new { message = "Xác thực không hợp lệ hoặc OTP đã hết hạn!" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy thông tin người dùng." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            
            await _context.SaveChangesAsync();
            
            _otpStorage.TryRemove(request.Email, out _);

            return Ok(new { message = "Mật khẩu đã được cập nhật thành công!" });
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.brevo.com/v3/");
                    
                    client.DefaultRequestHeaders.Add("api-key", "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");
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var emailData = new
                    {
                        sender = new { name = "H&Q Store", email = "diema448@gmail.com" },
                        to = new[] { new { email = toEmail, name = "Khách Hàng" } },
                        subject = subject,
                        htmlContent = $@"<h3>Mã OTP xác thực đặt lại mật khẩu của bạn là: <b style='color:blue; font-size:24px;'>{body}</b></h3>"
                    };

                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
                    };
                    
                    var jsonContent = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(emailData, options), 
                        System.Text.Encoding.UTF8, 
                        "application/json"
                    );

                    var response = await client.PostAsync("smtp/email", jsonContent);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo API Quên MK Error]: " + ex.Message);
                return false;
            }
        }
    }
}
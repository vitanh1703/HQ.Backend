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
        
        // Dùng ConcurrentDictionary để lưu trữ OTP tạm thời an toàn trong môi trường đa luồng
        // Key: Email -> Value: (Otp, ExpiryTime, FailedAttempts)
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
            // Kiểm tra xem email có tồn tại trong cơ sở dữ liệu không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null)
                return NotFound(new { message = "Email này không tồn tại trong hệ thống!" });

            // Chặn những tài khoản đăng ký qua Google
            if (user.AuthProvider == "google")
                return BadRequest(new { message = "Tài khoản này được đăng nhập bằng Google, không thể đổi mật khẩu qua hệ thống!" });

            string otp = new Random().Next(100000, 999999).ToString();
            // Lưu OTP có thời hạn 5 phút
            _otpStorage[request.Email] = (otp, DateTime.Now.AddMinutes(5), 0);

            bool isSent = await SendEmailAsync(request.Email, "Mã OTP đặt lại mật khẩu - H&Q Store", 
                $"Xin chào,\n\nMã OTP để đặt lại mật khẩu của bạn là: {otp}\n\nMã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.");

            if (isSent) 
                return Ok(new { message = "Mã OTP đã được gửi!" });
            
            return StatusCode(500, new { message = "Gửi mail thất bại, kiểm tra kết nối mạng hoặc cấu hình SMTP!" });
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
            // Bước này cần phải xác minh lại để tránh trường hợp gọi thẳng API reset-password bỏ qua bước Verify
            if (!_otpStorage.TryGetValue(request.Email, out var data) || data.Otp != request.Otp)
                return BadRequest(new { message = "Xác thực không hợp lệ hoặc OTP đã hết hạn!" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy thông tin người dùng." });

            // Mã hoá mật khẩu mới. Dựa vào Database, hệ thống bạn dùng Bcrypt
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            
            await _context.SaveChangesAsync();
            
            // Xóa bộ nhớ tạm sau khi đổi thành công
            _otpStorage.TryRemove(request.Email, out _);

            return Ok(new { message = "Mật khẩu đã được cập nhật thành công!" });
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("diema448@gmail.com", "gyykaypfhslrkvew");
                    smtp.EnableSsl = true;

                    MailMessage mail = new MailMessage
                    {
                        From = new MailAddress("diema448@gmail.com", "H&Q Store"),
                        Subject = subject,
                        Body = body
                    };
                    mail.To.Add(toEmail);

                    await smtp.SendMailAsync(mail);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
                return false;
            }
        }
    }
}
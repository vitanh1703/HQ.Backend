﻿using Google.Apis.Auth;
using HQ.Backend.Data;
using HQ.Backend.DTOs;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Net;
using System.Net.Mail;
using System.Collections.Concurrent;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry, int FailedAttempts, User PendingUser)> _registerOtpStorage = new();

        public class VerifyRegisterOtpDto { public string Email { get; set; } public string Otp { get; set; } }

        public AuthController(AppDbContext context) { _context = context; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "Email đã được sử dụng!" });

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.Now;
            user.Status = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("send-register-otp")]
        public async Task<IActionResult> SendRegisterOtp([FromBody] User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "Email đã được sử dụng!" });

            string otp = new Random().Next(100000, 999999).ToString();
            
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.Now;
            user.Status = true;
            user.Role = "Customer";

            _registerOtpStorage[user.Email] = (otp, DateTime.Now.AddMinutes(5), 0, user);

            bool isSent = await SendEmailAsync(user.Email, "Mã OTP đăng ký tài khoản - H&Q Store", 
                $"Xin chào,\n\nMã OTP để đăng ký tài khoản của bạn là: {otp}\n\nMã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.");

            if (isSent) 
                return Ok(new { message = "Mã OTP đã được gửi!" });
            
            return StatusCode(500, new { message = "Gửi mail thất bại, kiểm tra kết nối mạng hoặc cấu hình SMTP!" });
        }

        [HttpPost("verify-register-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyRegisterOtpDto request)
        {
            if (!_registerOtpStorage.TryGetValue(request.Email, out var data))
                return BadRequest(new { message = "OTP không tồn tại hoặc đã hết hạn." });

            if (DateTime.Now > data.Expiry)
            {
                _registerOtpStorage.TryRemove(request.Email, out _);
                return BadRequest(new { message = "OTP đã hết hạn." });
            }

            if (data.Otp != request.Otp)
            {
                int failedAttempts = data.FailedAttempts + 1;
                if (failedAttempts >= 5)
                {
                    _registerOtpStorage.TryRemove(request.Email, out _);
                    return BadRequest(new { message = "Bạn đã nhập sai quá 5 lần. Mã OTP đã bị hủy!" });
                }
                _registerOtpStorage[request.Email] = (data.Otp, data.Expiry, failedAttempts, data.PendingUser);
                return BadRequest(new { message = $"Mã OTP không đúng. Bạn còn {5 - failedAttempts} lần thử." });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                _registerOtpStorage.TryRemove(request.Email, out _);
                return BadRequest(new { message = "Email đã được sử dụng!" });
            }

            _context.Users.Add(data.PendingUser);
            await _context.SaveChangesAsync();

            _registerOtpStorage.TryRemove(request.Email, out _);

            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Models.LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác!" });
            }

            if (!user.Status)
            {
                return BadRequest(new { message = "Tài khoản của bạn đã bị khóa!" });
            }
            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "Customer"; 
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"User Role: {user.Role}"); 

            var fakeToken = "HQ-STORE-TOKEN-" + Guid.NewGuid().ToString();
            var userRole = user.Role ?? "Customer";

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = fakeToken,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    full_name = user.FullName,
                    role = userRole,
                    avatar = user.AvatarUrl,
                    address = user.Address,
                    phone = user.Phone
                }
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { "249381559845-1s30c3kjmaeic2v35il5vjqir9930pq2.apps.googleusercontent.com" }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null)
                {
                    user = new User
                    {
                        Email = payload.Email,
                        FullName = payload.Name,
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        Role = "Customer",
                        AuthProvider = "google",
                        GoogleId = payload.Subject,
                        AvatarUrl = payload.Picture,
                        Status = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                else if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "Customer";
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Đăng nhập Google thành công",
                    token = "HQ-STORE-GOOGLE-TOKEN-" + Guid.NewGuid().ToString(),
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        full_name = user.FullName,
                        role = user.Role ?? "Customer",
                        avatar = user.AvatarUrl,
                        address = user.Address,
                        phone = user.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Xác thực Google thất bại", error = ex.Message });
            }
        }

        [HttpGet("verify-admin/{id}")]
        public async Task<IActionResult> VerifyAdmin(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            if (string.IsNullOrEmpty(user.Role) || !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "Bạn không có quyền truy cập trang quản trị!" });
            }

            return Ok(new { message = "Xác thực Admin thành công!" });
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
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.GenAI;       // Thư viện SDK mới
using Google.GenAI.Types; // Namespace chứa cấu hình nội dung và cấu trúc AI

namespace HQ.Backend.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        [HttpPost("ask")]
        public async Task<IActionResult> AskChatbot([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(new { message = "Tin nhắn của khách hàng không được để trống." });

            try
            {
                // 1. Khởi tạo Client trần. 
                // SDK mới sẽ tự động bốc biến môi trường GEMINI_API_KEY đã cấu hình trên Railway.
                var client = new Client();

                // 2. Thiết lập System Instruction (Ngữ cảnh đóng vai) chuẩn cấu trúc đối tượng Content/Part của SDK
                var generateContentConfig = new GenerateContentConfig
                {
                    SystemInstruction = new Content
                    {
                        Parts = new List<Part> 
                        {
                            new Part { Text = "Bạn là trợ lý ảo thông minh của 'H&Q Store' - cửa hàng thời trang Streetwear. Hãy trả lời khách hàng bằng tiếng Việt một cách lịch sự, ngắn gọn dưới 3 câu." }
                        }
                    },
                    Temperature = 0.7, // Giữ độ sáng tạo vừa phải cho tư vấn thời trang
                    MaxOutputTokens = 150
                };

                // 3. Gọi mô hình thế hệ mới "gemini-2.0-flash" (hoặc gemini-1.5-flash-latest tùy gói cước tài khoản của bạn)
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash", // Hoặc "gemini-2.0-flash" theo tài liệu
                    contents: request.Message,
                    config: generateContentConfig
                );

                // 4. Bóc tách JSON lấy dữ liệu chuỗi text trả về theo chuẩn SDK mới
                string? botReply = response.Candidates?[0]?.Content?.Parts?[0]?.Text;

                if (string.IsNullOrEmpty(botReply))
                {
                    return StatusCode(500, new { message = "Không nhận được phản hồi hợp lệ từ mô hình AI." });
                }

                // Trả kết quả mượt mà về cho Front-end React hiển thị lên bong bóng chat
                return Ok(new { reply = botReply.Trim() });
            }
            catch (Exception ex)
            {
                // In log kiểm soát lỗi hạ tầng nếu có
                Console.WriteLine($"[GOOGLE GEN AI SDK ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi xử lý hệ thống Chatbot qua SDK mới", error = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
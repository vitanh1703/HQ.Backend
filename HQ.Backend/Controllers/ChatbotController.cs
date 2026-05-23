using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.GenAI;       // Khớp thư viện trong ảnh mẫu
using Google.GenAI.Types; // Khớp thư viện trong ảnh mẫu

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
                return BadRequest(new { message = "Tin nhắn không được để trống." });

            try
            {
                // 🎯 Điền trực tiếp API Key của bác vào đây để triệt tiêu lỗi môi trường trên Railway
                string myApiKey = "AIzaSyDazUCSsUfxtH0rmGmgFMch1igMOGSFo04";

                // 🎯 Khởi tạo Client truyền cứng Key theo chuẩn SDK
                var client = new Client(apiKey: myApiKey);

                // 🎯 Tạo Config đóng vai và cấu hình y hệt ảnh mẫu bác gửi
                var generateContentConfig = new GenerateContentConfig
                {
                    SystemInstruction = new Content
                    {
                        Parts = new List<Part> 
                        {
                            new Part { Text = "Bạn là trợ lý ảo thông minh của 'H&Q Store' - cửa hàng thời trang Streetwear. Hãy trả lời khách hàng bằng tiếng Việt một cách lịch sự, ngắn gọn dưới 3 câu." }
                        }
                    },
                    Temperature = 0.7,
                    MaxOutputTokens = 150
                };

                // 🎯 Gọi hàm GenerateContentAsync khớp chính xác từng tham số như ảnh mẫu
                // Sử dụng model quốc dân gemini-1.5-flash-latest để an toàn băng thông
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-1.5-flash", 
                    contents: request.Message,
                    config: generateContentConfig
                );

                // 🎯 Bóc tách dữ liệu JSON tầng sâu trả về từ Google
                string? botReply = response.Candidates?[0]?.Content?.Parts?[0]?.Text;
                
                return Ok(new { reply = botReply?.Trim() });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi phát sinh, in ra log và nhả chuỗi lỗi về Front-end để bác nhìn thấy ngay
                Console.WriteLine($"[CRITICAL ERROR]: {ex.Message}");
                return Ok(new { reply = $"[Mẫu Code SDK Error]: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HQ.Backend.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private static string GetApiKey()
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "api_key.txt");
                if (System.IO.File.Exists(path))
                {
                    return System.IO.File.ReadAllText(path).Trim();
                }
                var envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(envKey)) return envKey.Trim();

                return "KEY_NOT_FOUND";
            }
            catch { return ""; }
        }

        private static string GEMINI_URL => $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={GetApiKey()}";
        private static string _trainingData = "";
        private static List<string> _conversationHistory = new List<string>();
        private const int MaxHistoryEntries = 10;

        public ChatbotController()
        {
            if (string.IsNullOrEmpty(_trainingData))
            {
                try
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "ChatBot", "data.txt");
                    if (System.IO.File.Exists(filePath))
                    {
                        _trainingData = System.IO.File.ReadAllText(filePath);
                    }
                }
                catch { }
            }
        }

        public class ChatRequest
        {
            public string message { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.message))
                return BadRequest(new { message = "Tin nhắn không được để trống." });

            try
            {
                // Build history text (limited)
                var historyText = string.Join("\n", _conversationHistory);

                var prompt = _trainingData
                    + "\n\n--- LỊCH SỬ ĐỐI THOẠI: ---\n"
                    + historyText
                    + "\n\n--- CÂU HỎI CỦA KHÁCH HÀNG: ---\n"
                    + request.message
                    + "\n\nHãy trả lời đóng vai nhân viên H&Q Store như hướng dẫn.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                using var client = new HttpClient();
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(GEMINI_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new { reply = "Lỗi API Google: " + responseString });
                }

                using var doc = JsonDocument.Parse(responseString);
                string? botReply = null;
                try
                {
                    botReply = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();
                }
                catch
                {
                    botReply = responseString;
                }

                botReply = botReply?.Trim();

                // Append to in-memory history
                _conversationHistory.Add("User: " + request.message);
                if (!string.IsNullOrEmpty(botReply)) _conversationHistory.Add("Assistant: " + botReply);
                // Trim history
                if (_conversationHistory.Count > MaxHistoryEntries)
                {
                    var removeCount = _conversationHistory.Count - MaxHistoryEntries;
                    _conversationHistory.RemoveRange(0, removeCount);
                }

                return Ok(new { reply = botReply });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR]: {ex.Message}");
                return Ok(new { reply = $"[Mẫu Code HTTP Error]: {ex.Message}" });
            }
        }
    }
}
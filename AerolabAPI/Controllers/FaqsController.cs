using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using AerolabAPI.Services;


[Route("api/faqs")]
[ApiController]
public class FaqController : ControllerBase
{
    private readonly FaqService _faqService;
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;

    public FaqController(FaqService faqService, IConfiguration config)
    {
        _faqService = faqService;
        _httpClient = new HttpClient();
        _openAiApiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI API key is missing in appsettings.json");
    }

    // ✅ GET: /api/faqs → Get all predefined FAQs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Faq>>> GetAllFaqs()
    {
        var faqs = await _faqService.GetAllFaqsAsync();
        return Ok(faqs);
    }

    // ✅ POST: /api/faqs → Add new FAQ to MongoDB
    [HttpPost]
    public async Task<IActionResult> AddFaq([FromBody] Faq faq)
    {
        if (string.IsNullOrEmpty(faq.Question) || string.IsNullOrEmpty(faq.Answer))
        {
            return BadRequest(new { message = "Question and Answer are required." });
        }
        await _faqService.AddFaqAsync(faq);
        return CreatedAtAction(nameof(GetAllFaqs), new { id = faq.Id }, faq);
    }

    // ✅ GET: /api/faqs/search?query=your-question → Get Answer
    [HttpGet("search")]
    public async Task<ActionResult<string>> GetAnswer([FromQuery] string query)
    {
        // 🔹 Check MongoDB first for predefined response
        var faq = await _faqService.GetFaqByQuestionAsync(query);
        if (faq != null)
        {
            return Ok(new { answer = faq.Answer, source = "Predefined" });
        }

        // 🔹 If not found, query OpenAI API
        var aiResponse = await QueryOpenAi(query);
        if (!string.IsNullOrEmpty(aiResponse))
        {
            return Ok(new { answer = aiResponse, source = "AI" });
        }

        return NotFound(new { message = "No answer found." });
    }

    // ✅ OpenAI API Query Function
    private async Task<string> QueryOpenAi(string prompt)
    {
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful AI assistant answering predefined questions and interacting with people." },
                new { role = "user", content = prompt }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No valid response received.";
        }

        return "Sorry, I couldn't fetch a response from OpenAI.";
    }
     [HttpPost("voice")]
    public async Task<ActionResult<string>> ProcessVoiceQuery([FromBody] VoiceRequest voiceRequest)
    {
        if (voiceRequest == null || string.IsNullOrEmpty(voiceRequest.Query))
        {
            return BadRequest(new { message = "Invalid voice input." });
        }

        var response = await GetAnswer(voiceRequest.Query);
        return response;
    }
}
public class VoiceRequest
{
    public required string Query { get; set; }  // ✅ Fix
}
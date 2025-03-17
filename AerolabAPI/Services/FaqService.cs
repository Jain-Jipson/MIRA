using Microsoft.ML;
using Microsoft.ML.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AerolabAPI.Services  // ✅ Ensure it's inside a namespace
{
    public class FaqService
    {
        private readonly IMongoCollection<Faq> _faqsCollection;
        private readonly MLContext _mlContext;
        private PredictionEngine<FaqInput, FaqPrediction>? _predictionEngine;
        private ITransformer? _model;
        private readonly string _openAiApiKey;
        private readonly HttpClient _httpClient;

        public FaqService(IOptions<MongoDbSettings> settings, IConfiguration configuration)
        {
            if (settings?.Value == null || string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentNullException(nameof(settings.Value.ConnectionString), "MongoDB connection string is missing.");
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _faqsCollection = database.GetCollection<Faq>(settings.Value.FaqCollectionName);

            _mlContext = new MLContext();
            _httpClient = new HttpClient();

            _openAiApiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                throw new ArgumentNullException(nameof(_openAiApiKey), "OpenAI API key is missing.");
            }

            TrainModel();
        }

        private void TrainModel()
        {
            var data = _faqsCollection.Find(_ => true).ToList();

            if (data.Count < 2)
            {
                Console.WriteLine("Not enough data for training.");
                return;
            }

            var trainingData = data.Select(faq => new FaqInput { Question = faq.Question, Answer = faq.Answer }).ToList();
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(FaqInput.Question))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(FaqInput.Answer)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(nameof(FaqPrediction.Answer), "Label"));

            _model = pipeline.Fit(dataView);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<FaqInput, FaqPrediction>(_model);
        }

        public async Task<Faq> GetFaqByQuestionAsync(string query)
{
    try
    {
        var faq = await _faqsCollection.Find(f => f.Question.ToLower() == query.ToLower()).FirstOrDefaultAsync();
        if (faq != null) return faq;

        if (_predictionEngine != null)
        {
            var prediction = _predictionEngine.Predict(new FaqInput { Question = query });
            if (!string.IsNullOrWhiteSpace(prediction.Answer))
            {
                return new Faq { Question = query, Answer = prediction.Answer, Category = "ML Prediction" };
            }
        }

        return await GetOpenAIResponse(query) ?? new Faq { Question = query, Answer = "I couldn't find an answer.", Category = "AI Generated" };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error in GetFaqByQuestionAsync: {ex.Message}");
        return new Faq { Question = query, Answer = "An error occurred while processing your request.", Category = "Error" };
    }
}


        public async Task<List<Faq>> GetAllFaqsAsync()
        {
            return await _faqsCollection.Find(_ => true).ToListAsync();
        }

        private async Task<Faq> GetOpenAIResponse(string query)
        {
            try
            {
                var openAiRequest = new
                {
                    model = "gpt-4-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are an AI receptionist. Answer user queries based on your training." },
                        new { role = "user", content = query }
                    }
                };

                var jsonRequest = JsonSerializer.Serialize(openAiRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonResponse);
                    var answer = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    return new Faq { Question = query, Answer = answer ?? "I don't know the answer yet!", Category = "AI Generated" };
                }
                else
                {
                    Console.WriteLine($"❌ OpenAI API Error: {response.StatusCode}");
                    return new Faq { Question = query, Answer = "I couldn't fetch an answer right now.", Category = "AI Generated" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception calling OpenAI: {ex.Message}");
                return new Faq { Question = query, Answer = "I encountered an error processing this request.", Category = "AI Error" };
            }
        }

        public async Task AddFaqAsync(Faq faq)
        {
            await _faqsCollection.InsertOneAsync(faq);
        }
    }

    // ✅ Define Input and Output Schema
    public class FaqInput
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public class FaqPrediction
    {
        [ColumnName("Answer")] // Ensure it matches the mapped output column
        public string Answer { get; set; } = "";
    }
}

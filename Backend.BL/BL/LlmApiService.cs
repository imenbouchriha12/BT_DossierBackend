using Backend.BL.IBL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.BL.BL
{
    /// <summary>
    /// Client HTTP vers un fournisseur LLM compatible OpenAI (chat/completions).
    /// La clé API, l'URL de base et le modèle sont lus depuis la configuration
    /// (section Llm, surchargeable via LLM__APIKEY, LLM__BASEURL, LLM__MODEL, LLM__TIMEOUTSECONDS).
    /// Aucun appel n'est effectué si la clé n'est pas renseignée : CompleteAsync retourne null.
    /// </summary>
    public class LlmApiService : ILlmApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlmApiService> _logger;

        public LlmApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LlmApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            double temperature = 0.5,
            int maxTokens = 1024,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Llm:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Clé API LLM non configurée (Llm:ApiKey).");
                return null;
            }

            var baseUrl = _configuration["Llm:BaseUrl"] ?? "https://api.groq.com/openai/v1";
            var model = _configuration["Llm:Model"] ?? "llama-3.3-70b-versatile";
            var timeoutSeconds = int.TryParse(_configuration["Llm:TimeoutSeconds"], out var t) ? t : 60;

            var payload = new LlmChatRequest
            {
                Model = model,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Messages = new[]
                {
                    new LlmMessage { Role = "system", Content = systemPrompt },
                    new LlmMessage { Role = "user", Content = userPrompt }
                }
            };

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload, LlmJsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cts.Token);
                var json = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Le fournisseur LLM a répondu {StatusCode} pour le modèle {Model} : {Body}",
                        response.StatusCode, model, json);
                    return null;
                }

                var result = JsonSerializer.Deserialize<LlmChatResponse>(json, LlmJsonOptions);

                var content = result?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("Réponse LLM vide pour le modèle {Model}.", model);
                    return null;
                }

                return content.Trim();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Échec de l'appel au fournisseur LLM (modèle {Model}).", model);
                return null;
            }
        }

        private static readonly JsonSerializerOptions LlmJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private sealed class LlmChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public LlmMessage[] Messages { get; set; } = Array.Empty<LlmMessage>();

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0.5;

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; } = 1024;
        }

        private sealed class LlmMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class LlmChatResponse
        {
            [JsonPropertyName("choices")]
            public LlmChoice[]? Choices { get; set; }
        }

        private sealed class LlmChoice
        {
            [JsonPropertyName("message")]
            public LlmMessage? Message { get; set; }
        }
    }
}

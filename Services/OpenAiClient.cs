using PocGestorExpectativas.Models;
using System.Text;
using System.Text.Json;
using PocGestorExpectativas.Models.Expectations;
using PocGestorExpectativas.Services.Interfaces;

namespace PocGestorExpectativas.Services;

public class OpenAiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        var apiKey = _configuration["LlmSettings:OpenAi:OpenAiApiKey"];
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<string> NormalizeBeneficiaryAsync(string rawBeneficiaryName, CancellationToken cancellationToken = default)
    {
        var prompt = $@"
                    Normalize este nome de beneficiário para um identificador único e consistente:
                    Nome original: ""{rawBeneficiaryName}""

                    Regras:
                    - Use sempre MAIÚSCULAS
                    - Remova acentos e caracteres especiais
                    - Agrupe variações do mesmo beneficiário
                    - Exemplos: ""CEMIG DISTRIBUICAO"" -> ""CEMIG"", ""Cemig Energia"" -> ""CEMIG""

                    Retorne apenas o nome normalizado:";

        return await CallOpenAiAsync(prompt, cancellationToken);
    }

    public async Task<ExpectationResult> AnalyzePaymentHistoryAsync(string normalizedBeneficiary, List<Payment> history, CancellationToken cancellationToken = default)
    {
        var historyJson = JsonSerializer.Serialize(history.Select(p => new {
            p.Value,
            p.DueDate,
            p.PaidAt,
            p.CreatedAt
        }), new JsonSerializerOptions { WriteIndented = true });

        var prompt = $@"
                    Analise o histórico de pagamentos do beneficiário ""{normalizedBeneficiary}"" e sugira a próxima expectativa de pagamento.

                    Histórico dos últimos pagamentos:
                    {historyJson}

                    Retorne apenas JSON com:
                    {{
                      ""nextExpectedPaymentDate"": ""YYYY-MM-DD"",
                      ""nextExpectedAmount"": decimal,
                      ""confidenceScore"": 0.0-1.0,
                      ""rationale"": ""explicação em português""
                    }}

                    Considere:
                    - Padrões de sazonalidade
                    - Frequência de pagamentos
                    - Valores típicos
                    - Atrasos históricos";
        
        var response = await CallOpenAiAsync(prompt, cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<ExpectationResult>(response) ?? new ExpectationResult();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao parsear resposta da IA: {Response}", response);
            return new ExpectationResult
            {
                ConfidenceScore = 0.1,
                Rationale = "Erro ao processar análise da IA"
            };
        }
    }

    private async Task<string> CallOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _configuration["LlmSettings:OpenAi:Model"],
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = double.Parse(_configuration["LlmSettings:OpenAi:Temperature"] ?? "0.1"),
            max_tokens = 500
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
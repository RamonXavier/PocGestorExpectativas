using PocGestorExpectativas.Models;
using System.Text;
using System.Text.Json;
using PocGestorExpectativas.Models.Expectations;
using PocGestorExpectativas.Services.Interfaces;

namespace PocGestorExpectativas.Services;

public class GroqClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqClient> _logger;

    public GroqClient(HttpClient httpClient, IConfiguration configuration, ILogger<GroqClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        var apiKey = _configuration["LlmSettings:Groq:GroqApiKey"];
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

        return await CallGroqAsync(prompt, cancellationToken);
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

                    Atenção, a regra mais importante:Não coloque nenhum texto fora do json especificado a seguir. Retorne apenas o seguinte JSON preenchido:
                    {{
                      ""nextExpectedPaymentDate"": ""YYYY-MM-DD"",
                      ""nextExpectedAmount"": decimal,
                      ""confidenceScore"": 0.0-1.0,
                      ""rationale"": ""explicação em português""
                    }}
                    
                    Qualquer outro texto que queria colocar, deve ficar dentro da propriedade rationale do json indicado.

                    Exemplo de retorno incorreto:
                    
                    'Este texto deveria estar dentro do json'
                    {{
                      ""nextExpectedPaymentDate"": ""2025-10-22"",
                      ""nextExpectedAmount"": 89.90,
                      ""confidenceScore"": 0.2,
                      ""rationale"": ""Considerando o padrão observado, onde dois pagamentos de mesmo valor ocorreram no mesmo dia, é difícil estabelecer um padrão de frequência ou sazonalidade. A falta de datas de vencimento nos pagamentos históricos também limita a capacidade de prever quando o próximo pagamento deve ocorrer. A previsão de data e valor do próximo pagamento é baseada na simples repetição do valor e na suposição de que, se houvesse um padrão diário, o próximo pagamento seria no dia seguinte. A confiança na previsão é baixa devido à limitação dos dados.""
                    }}

                    Considere:
                    - Padrões de sazonalidade
                    - Frequência de pagamentos
                    - Valores típicos
                    - Atrasos históricos";

        var response =
            "{\n  \"nextExpectedPaymentDate\": \"2025-10-22\",\n  \"nextExpectedAmount\": 483.74,\n  \"confidenceScore\": 0.2,\n  \"rationale\": \"Considerando o padrão observado, onde apenas um pagamento foi registrado, é difícil estabelecer um padrão de frequência ou sazonalidade. A falta de datas de vencimento nos pagamentos históricos também limita a capacidade de prever quando o próximo pagamento deve ocorrer. A previsão de data e valor do próximo pagamento é baseada na simples repetição do valor e na suposição de que, se houvesse um padrão diário, o próximo pagamento seria no dia seguinte. A confiança na previsão é baixa devido à limitação dos dados. Este único pagamento não oferece informações suficientes para identificar padrões de sazonalidade, frequência de pagamentos ou atrasos históricos, o que torna a previsão do próximo pagamento altamente incerta.\"\n}";
        //await CallGroqAsync(prompt, cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<ExpectationResult>(response.Replace("```","")) ?? new ExpectationResult();
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

    private async Task<string> CallGroqAsync(string prompt, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _configuration["LlmSettings:Groq:Model"] ?? "llama-3.1-70b-versatile", // Modelo padrão da Groq
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = double.Parse(_configuration["LlmSettings:Groq:Temperature"] ?? "0.1"),
            max_tokens = 500,
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return responseObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar Groq API");
            throw;
        }
    }
}
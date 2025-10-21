using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;
using PocGestorExpectativas.Services;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using PocGestorExpectativas.Models.IA;

namespace PocGestorExpectativas.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private readonly ExpectationAnalyzer _expectationAnalyzer;
    private readonly AppDbContext _context;

    public TestController(ILogger<TestController> logger, IOptions<RabbitMQSettings> rabbitMQSettings, ExpectationAnalyzer expectationAnalyzer, AppDbContext context)
    {
        _logger = logger;
        _expectationAnalyzer = expectationAnalyzer;
        _context = context;
        _rabbitMQSettings = rabbitMQSettings.Value;
    }

    /// <summary>
    /// Envia uma mensagem de teste para a fila RabbitMQ
    /// </summary>
    [HttpPost("send-payment")]
    public ActionResult SendTestPayment([FromBody] PaymentMessage paymentMessage)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMQSettings.ConnectionString)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Declarar a fila
            channel.QueueDeclare(
                queue: _rabbitMQSettings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Serializar mensagem
            var message = JsonSerializer.Serialize(paymentMessage);
            var body = Encoding.UTF8.GetBytes(message);

            // Publicar mensagem
            channel.BasicPublish(
                exchange: "",
                routingKey: _rabbitMQSettings.QueueName,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Mensagem enviada para a fila: {Message}", message);

            return Ok(new { message = "Mensagem enviada com sucesso", data = paymentMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para RabbitMQ");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cria dados de teste pré-definidos
    /// </summary>
    [HttpPost("create-sample-data")]
    public ActionResult CreateSampleData()
    {
        var samplePayments = new[]
        {
            new PaymentMessage
            {
                IdentificationField = "23791234567890123456789012345678901234567890",
                Value = 89.90m,
                DueDate = DateTime.Now.AddDays(5),
                BeneficiaryName = "COPASA MG"
            },
            new PaymentMessage
            {
                IdentificationField = "34198765432109876543210987654321098765432109",
                Value = 156.75m,
                DueDate = DateTime.Now.AddDays(3),
                BeneficiaryName = "CEMIG DISTRIBUICAO"
            },
            new PaymentMessage
            {
                IdentificationField = "10412345678901234567890123456789012345678901",
                Value = 45.30m,
                DueDate = DateTime.Now.AddDays(7),
                BeneficiaryName = "SABESP"
            }
        };

        var results = new List<object>();

        foreach (var payment in samplePayments)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_rabbitMQSettings.ConnectionString)
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(
                    queue: _rabbitMQSettings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var message = JsonSerializer.Serialize(payment);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(
                    exchange: "",
                    routingKey: _rabbitMQSettings.QueueName,
                    basicProperties: null,
                    body: body);

                results.Add(new { success = true, beneficiary = payment.BeneficiaryName });
            }
            catch (Exception ex)
            {
                results.Add(new { success = false, beneficiary = payment.BeneficiaryName, error = ex.Message });
            }
        }

        return Ok(new { message = "Dados de teste enviados", results });
    }

    [HttpPost("test-ai")]
    public async Task<IActionResult> TestAi([FromBody] TestAiRequest request)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BeneficiaryName = request.BeneficiaryName,
            Value = request.Value,
            IdentificationField = Guid.NewGuid().ToString(),
            Pago = true,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        await _expectationAnalyzer.AnalyzeAndCreateExpectationAsync(payment);

        return Ok(new
        {
            Message = "Análise de IA concluída",
            PaymentId = payment.Id
        });
    }

    [HttpGet("expectations")]
    public async Task<IActionResult> GetExpectations()
    {
        var expectations = await _context.Expectations
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .ToListAsync();

        return Ok(expectations);
    }
}
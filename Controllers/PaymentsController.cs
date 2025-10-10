using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;

namespace PocGestorExpectativas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(AppDbContext context, ILogger<PaymentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os pagamentos
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments([FromQuery] bool? pago = null)
    {
        var query = _context.Payments.AsQueryable();

        if (pago.HasValue)
        {
            query = query.Where(p => p.Pago == pago.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(payments);
    }

    /// <summary>
    /// Obtém um pagamento específico por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    /// <summary>
    /// Cria um novo pagamento
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        payment.Id = Guid.NewGuid();
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Action = "payment_created",
            Details = $"Pagamento criado: {payment.BeneficiaryName} - R$ {payment.Value:F2}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pagamento criado: {PaymentId} para {Beneficiary}", payment.Id, payment.BeneficiaryName);

        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
    }

    /// <summary>
    /// Atualiza um pagamento
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(Guid id, Payment payment)
    {
        if (id != payment.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingPayment = await _context.Payments.FindAsync(id);
        if (existingPayment == null)
        {
            return NotFound();
        }

        // Atualizar campos
        existingPayment.IdentificationField = payment.IdentificationField;
        existingPayment.Value = payment.Value;
        existingPayment.DueDate = payment.DueDate;
        existingPayment.BeneficiaryName = payment.BeneficiaryName;
        existingPayment.NormalizedBeneficiary = payment.NormalizedBeneficiary;
        existingPayment.Pago = payment.Pago;
        existingPayment.PaidAt = payment.PaidAt;
        existingPayment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Action = "payment_updated",
            Details = $"Pagamento atualizado: {payment.BeneficiaryName} - R$ {payment.Value:F2}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pagamento atualizado: {PaymentId}", payment.Id);

        return NoContent();
    }

    /// <summary>
    /// Deleta um pagamento
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Action = "payment_deleted",
            Details = $"Pagamento deletado: {payment.BeneficiaryName} - R$ {payment.Value:F2}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pagamento deletado: {PaymentId}", payment.Id);

        return NoContent();
    }
}

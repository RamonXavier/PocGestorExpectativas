# Planejamento POC - Gestor de Expectativas de Faturas

## 🎯 Objetivo Atualizado
Criar uma POC que consome faturas via RabbitMQ, processa pagamentos e usa IA real para gerar expectativas de próximos pagamentos baseado no histórico.

## 🔄 Fluxo de Dados Atualizado

```
[Faturas Externas] -> [RabbitMQ] -> [Worker] -> [Banco] -> [RAG Normalizer] -> [IA] -> [Expectativas]
                                                      |                           |
                                                 [Status: Pago = true]    [Histórico Agrupado]
```

## 📋 Arquitetura Simplificada

### ✅ Mantido (essencial)
- API básica para CRUD
- Worker background simples
- Modelos de dados essenciais
- LLM adapter plugável
- Logs de auditoria básicos
- Sistema de expectativas
- **RabbitMQ para consumo de faturas**
- **IA real (OpenAI) para análise**

## 🏗️ Arquitetura Atualizada

```
[Faturas Externas] -> [RabbitMQ] -> [Worker Background] -> [SQL Server]
                                                              |
                                                         [IA Analysis] -> [Expectativas]
                                                              |
                                                         [OpenAI/Local LLM]
```

## 📊 Modelos de Dados (Atualizados)

### Payment (Fatura/Pagamento)
```csharp
public class Payment
{
    public Guid Id { get; set; }
    public string IdentificationField { get; set; } // linha digitável
    public decimal Value { get; set; }
    public DateTime? DueDate { get; set; }
    public string BeneficiaryName { get; set; } // Nome original da fatura
    public string NormalizedBeneficiary { get; set; } // Nome normalizado via RAG
    public bool Pago { get; set; } = false; // Status principal
    public DateTime? PaidAt { get; set; } // Quando foi pago
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Expectation (Expectativa de Próximo Pagamento)
```csharp
public class Expectation
{
    public Guid Id { get; set; }
    public string NormalizedBeneficiary { get; set; } // Beneficiário normalizado via RAG
    public DateTime? NextExpectedPaymentDate { get; set; }
    public decimal? NextExpectedAmount { get; set; }
    public double ConfidenceScore { get; set; } // 0..1
    public string Rationale { get; set; } // Explicação da IA
    public string AnalysisMethod { get; set; } // "llm", "ml", "rule-based"
    public int HistoryCount { get; set; } // Quantos pagamentos foram analisados
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### AuditLog
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ExpectationId { get; set; }
    public string Action { get; set; } // "payment_received", "expectation_generated", "error"
    public string Details { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 🚀 Plano de Implementação (4 Fases)

### **FASE 1: Base + RabbitMQ (Semana 1)**
**Objetivo**: Sistema de filas funcionando

#### Tarefas:
1. **Criar projeto .NET 8**
   ```bash
   dotnet new webapi -n GestorExpectativasPoc
   cd GestorExpectativasPoc
   ```

2. **Configurar EF Core + SQL Server**
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.EntityFrameworkCore.Tools
   ```

3. **Configurar RabbitMQ**
   ```bash
   dotnet add package RabbitMQ.Client
   ```

4. **Criar modelos básicos** (Payment, Expectation, AuditLog)

5. **Configurar DbContext e Migration**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

6. **Criar endpoints básicos**
   - `GET /payments` - listar pagamentos
   - `GET /payments/{id}` - detalhar pagamento
   - `GET /expectations` - listar expectativas

#### ✅ Critério de Sucesso:
- API rodando e conectada ao banco
- RabbitMQ configurado e funcionando

---

### **FASE 2: Consumer RabbitMQ (Semana 2)**
**Objetivo**: Processar faturas da fila

#### Tarefas:
1. **Criar Consumer RabbitMQ**
   ```csharp
   public class PaymentConsumer : BackgroundService
   {
       // Consome fila de faturas
       // Atualiza status para Pago = true
   }
   ```

2. **Implementar lógica de processamento**
   - Consumir mensagens da fila
   - Criar/atualizar Payment no banco
   - Marcar como Pago = true
   - Log de auditoria

3. **Configurar RabbitMQ no Program.cs**
   ```csharp
   builder.Services.AddHostedService<PaymentConsumer>();
   ```

#### ✅ Critério de Sucesso:
- Worker consome fila automaticamente
- Pagamentos são marcados como Pago = true

---

### **FASE 3: RAG + IA Real para Análise (Semana 3)**
**Objetivo**: RAG normaliza beneficiários e IA analisa histórico agrupado

#### Tarefas:
1. **Criar BeneficiaryNormalizer (RAG)**
   ```csharp
   public class BeneficiaryNormalizer
   {
       public async Task<string> NormalizeAsync(string rawName)
       {
           // Usa LLM para normalizar nomes como "CEMIG DISTRIBUICAO" -> "CEMIG"
           // Cache para evitar chamadas desnecessárias
       }
   }
   ```

2. **Criar PaymentHistoryService**
   ```csharp
   public class PaymentHistoryService
   {
       public async Task<List<Payment>> GetBeneficiaryHistoryAsync(string normalizedBeneficiary)
       {
           // Busca histórico agrupado por beneficiário normalizado
           // Últimos 20 pagamentos do mesmo beneficiário
       }
   }
   ```

3. **Criar ILlmClient para IA real**
   ```csharp
   public interface ILlmClient
   {
       Task<ExpectationResult> AnalyzePaymentHistoryAsync(string normalizedBeneficiary, List<Payment> history);
   }
   ```

4. **Implementar OpenAiClient**
   - Configurar chave da OpenAI
   - Prompt engineering para análise de histórico agrupado
   - Análise de padrões de pagamento por beneficiário

5. **Criar ExpectationAnalyzer com RAG**
   ```csharp
   public class ExpectationAnalyzer
   {
       // 1. Normaliza nome do beneficiário via RAG
       // 2. Busca histórico agrupado
       // 3. Chama IA para análise
       // 4. Gera expectativa de próximo pagamento
   }
   ```

6. **Integrar no Worker**
   - Após marcar como Pago = true
   - Normalizar beneficiário via RAG
   - Disparar análise de expectativa com histórico agrupado
   - Salvar resultado na tabela Expectation

#### ✅ Critério de Sucesso:
- RAG normaliza beneficiários corretamente
- IA analisa histórico agrupado por beneficiário
- Gera expectativas baseadas em padrões consistentes
- Sistema funciona com OpenAI

---

### **FASE 4: Refinamento e Monitoramento (Semana 4)**
**Objetivo**: Sistema completo e observável

#### Tarefas:
1. **Adicionar logs detalhados**
   ```csharp
   _logger.LogInformation("Processando fatura {PaymentId} para {Beneficiary}", payment.Id, payment.BeneficiaryName);
   ```

2. **Endpoint de monitoramento**
   - `GET /admin/stats` - estatísticas do sistema
   - `GET /admin/queue-status` - status da fila

3. **Melhorar prompts de IA**
   - Análise de sazonalidade
   - Padrões de atraso
   - Confiança na previsão

4. **Testes com dados reais**
   - Simular faturas via RabbitMQ
   - Validar expectativas geradas

#### ✅ Critério de Sucesso:
- Sistema completo funcionando
- IA gerando expectativas realistas
- Monitoramento funcionando

## 🛠️ Estrutura de Projeto Atualizada

```
GestorExpectativasPoc/
├── Controllers/
│   ├── PaymentsController.cs
│   └── ExpectationsController.cs
├── Models/
│   ├── Payment.cs
│   ├── Expectation.cs
│   └── AuditLog.cs
├── Services/
│   ├── PaymentConsumer.cs (RabbitMQ)
│   ├── BeneficiaryNormalizer.cs (RAG)
│   ├── PaymentHistoryService.cs
│   ├── ExpectationAnalyzer.cs
│   └── LlmService.cs
├── Data/
│   └── AppDbContext.cs
├── Interfaces/
│   └── ILlmClient.cs
├── Program.cs
└── appsettings.json
```

## 📝 Endpoints Essenciais

```csharp
// Listar pagamentos
GET /payments
GET /payments?pago=true

// Listar expectativas
GET /expectations
GET /expectations?beneficiary={nome}

// Monitoramento
GET /admin/stats
GET /admin/queue-status
```

## 🔧 Configurações Atualizadas

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GestorExpectativas;Trusted_Connection=true;"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "QueueName": "faturas"
  },
  "LlmSettings": {
    "Provider": "OpenAI",
    "OpenAiApiKey": "sua-chave-aqui",
    "Model": "gpt-4o-mini",
    "Temperature": 0.1
  }
}
```

## 🤖 Prompt Engineering para IA

### Template de Normalização (RAG)
```
Normalize este nome de beneficiário para um identificador único e consistente:
Nome original: "{rawName}"

Regras:
- Use sempre MAIÚSCULAS
- Remova acentos e caracteres especiais
- Agrupe variações do mesmo beneficiário
- Exemplos: "CEMIG DISTRIBUICAO" -> "CEMIG", "Cemig Energia" -> "CEMIG"

Retorne apenas o nome normalizado:
```

### Template de Análise (com RAG)
```
Analise o histórico de pagamentos do beneficiário "{NormalizedBeneficiary}" e sugira a próxima expectativa de pagamento.

Histórico dos últimos pagamentos (agrupados por beneficiário normalizado):
{PAYMENT_HISTORY}

Retorne apenas JSON com:
{
  "nextExpectedPaymentDate": "YYYY-MM-DD",
  "nextExpectedAmount": decimal,
  "confidenceScore": 0.0-1.0,
  "rationale": "explicação em português"
}

Considere:
- Padrões de sazonalidade
- Frequência de pagamentos
- Valores típicos
- Atrasos históricos
- Consistência do beneficiário (histórico agrupado)
```

## 🎯 Próximos Passos Imediatos

1. **Criar projeto .NET 8**
2. **Configurar RabbitMQ local**
3. **Implementar modelos básicos**
4. **Criar consumer da fila**
5. **Implementar RAG (BeneficiaryNormalizer)**
6. **Configurar OpenAI**
7. **Testar fluxo completo com RAG**

## 📚 Recursos de Aprendizado

- **RabbitMQ**: Documentação oficial
- **OpenAI API**: Documentação OpenAI
- **RAG (Retrieval-Augmented Generation)**: Guias de implementação
- **Prompt Engineering**: Guias de análise de dados
- **Background Services**: Microsoft Learn

---

**Meta**: Ter uma POC funcional que consome faturas via RabbitMQ, usa RAG para normalizar beneficiários e IA real para gerar expectativas de próximos pagamentos baseadas em histórico agrupado.

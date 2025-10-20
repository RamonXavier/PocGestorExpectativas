# POC: Gestor de Expectativas de Faturas

> Documento para orientar a implementação da POC em .NET 8 — código base, estrutura, fluxos, prompts, critérios de aceitação e recomendações de modelos LLM (incluindo opções gratuitas / locais).
> Baseado no objetivo inicial fornecido: “quero criar uma poc de conceito, um gestor de expectativas de faturas que rode em background e gere sugestões de pagamento para faturas com `expectativaCalculada = false`”.
> Estou estudando então precisamos ir passo a passo para aprendr e evoluir sobre uso de IA.
---

## 1 — Visão geral / objetivo

Criar uma **POC** que consome registros de faturas/pagamentos, detecta entradas sem expectativa calculada e gera automaticamente **Expectativas de Pagamento** (data esperada, valor esperado, score de confiança e justificativa). O sistema deve rodar como um agente em background (timer/filas) e persistir sugestões em uma tabela separada, atualizando o registro original para evitar reprocessamento. A solução deve ser explicável, auditável e preparada para evoluir de rule-based → LLM → modelo preditivo (ML.NET) com RAG quando necessário.
Exemplo de pagamento: "{
  "minimumScheduleDate": "2025-10-09",
  "fee": 0,
  "bankSlipInfo": {
    "identificationField": "836400000045837401380055515194495112081437529738",
    "value": 483.74,
    "dueDate": null,
    "companyName": "CEMIG DISTRIBUICAO",
    "beneficiaryCpfCnpj": null,
    "beneficiaryName": "CEMIG DISTRIBUICAO",
    "allowChangeValue": false,
    "minValue": 483.74,
    "maxValue": 483.74,
    "discountValue": null,
    "interestValue": null,
    "fineValue": null,
    "originalValue": 483.74,
    "totalDiscountValue": null,
    "totalAdditionalValue": null,
    "isOverdue": false,
    "bank": null
  }
}"
---

## 2 — Critérios de aceitação (POC)

* O worker processa um lote (ex.: 100) de pagamentos com `expectativaCalculada = false` sem travamentos.
* Para cada pagamento processado gera-se uma `Expectation` com:

  * `expectedPaymentDate` (ou `null` se não for possível)
  * `expectedAmount`
  * `confidenceScore` (0..1)
  * `rationale` (máx 2-3 frases)
* O pagamento original é atualizado: `expectativaCalculada = true` e `ExpectationId` preenchido.
* Todas as decisões são logadas com metadata: `modelVersion`, `provider`, `timestamp`.
* Há um endpoint para revisão humana (aceitar/rejeitar) e feedback é persistido.
* POC deve rodar localmente com docker-compose (API + DB + Worker).

---

## 3 — Requisitos funcionais (resumo)

* Ingestão CRUD de pagamentos (API).
* Worker background que processa pagamentos pendentes em batch.
* Adapter para LLMs (ILlmClient) com implementação fake para testes e implementação real para OpenAI / local LLM.
* Persistência: tabelas `Payments`, `Expectations`, `AuditLogs`, `Feedbacks`.
* Endpoint para visualizar e corrigir expectativas.
* Mecanismo simples de RAG (opcional na POC): consulta ao histórico mais recente.

---

## 4 — Arquitetura (visão de alto nível)

```
[Client] -> [API ASP.NET Core] -> [SQL DB]
                          \
                           -> [Message Bus / Scheduler] -> [Worker (BackgroundService)]
                                                          -> [LLM Adapter] -> [LLM (local or cloud)]
                                                          -> [Vetor DB (RAG) - opcional]
```

Componentes principais:

* **Api** (ASP.NET Core 8): endpoints para CRUD de payments, list expectations, feedback.
* **Worker** (IHostedService / BackgroundService): processa pagamentos em lotes.
* **Db**: SQL Server / Postgres via EF Core.
* **LLM Adapter**: ILlmClient (pluggable).
* **(Opcional)** Qdrant / Pinecone para RAG.
* **Observability**: Serilog + Application Insights/Seq.

---

## 5 — Estrutura de repositório (sugestão)

```
GestorExpectativas.IA.POC/
├─ src/
│  ├─ Api/
│  │  ├─ Controllers/
│  │  ├─ Program.cs
│  │  └─ appsettings.Development.json
│  ├─ Worker/
│  │  ├─ Services/
│  │  │  ├─ ExpectationWorker.cs
│  │  │  └─ ExpectationService.cs
│  │  └─ Program.cs
│  ├─ Core/
│  │  ├─ Models/
│  │  │  ├─ Payment.cs
│  │  │  ├─ Expectation.cs
│  │  │  └─ AuditLog.cs
│  │  └─ DTOs/
│  ├─ Infrastructure/
│  │  ├─ Data/
│  │  │  └─ GestorDbContext.cs
│  │  └─ Repositories/
│  ├─ Integrations/
│  │  └─ Llm/
│  │     ├─ ILlmClient.cs
│  │     ├─ OpenAiClient.cs
│  │     └─ LocalLlmClient.cs
│  └─ Tests/
├─ docs/
│  └─ poc.md        (este arquivo)
├─ docker/
│  └─ docker-compose.yml
└─ README.md
```

---

## 6 — Modelos de dados (EF Core) — código exemplo

**Core/Models/Payment.cs**

```csharp
public class Payment
{
    public Guid Id { get; set; }
    public string IdentificationField { get; set; } // ex: linha digitável
    public decimal OriginalValue { get; set; }
    public decimal Value { get; set; }
    public DateTime? DueDate { get; set; }
    public string BeneficiaryName { get; set; }
    public bool ExpectationCalculated { get; set; } = false;
    public Guid? ExpectationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Core/Models/Expectation.cs**

```csharp
public class Expectation
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public DateTime? ExpectedPaymentDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public double ConfidenceScore { get; set; } // 0..1
    public string Source { get; set; } // "rule-based" | "llm" | "mlnet"
    public string Rationale { get; set; } // explicação curta
    public string ModelMeta { get; set; } // ex: "openai:gpt-4o-mini:t0.1"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Core/Models/AuditLog.cs**

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? ExpectationId { get; set; }
    public string Action { get; set; } // "suggested", "accepted", "rejected"
    public string Details { get; set; } // raw llm response, errors, etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Infrastructure/Data/GestorDbContext.cs**

```csharp
public class GestorDbContext : DbContext
{
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Expectation> Expectations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public GestorDbContext(DbContextOptions<GestorDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>().HasKey(p => p.Id);
        modelBuilder.Entity<Expectation>().HasKey(e => e.Id);
        modelBuilder.Entity<AuditLog>().HasKey(a => a.Id);
    }
}
```

---

## 7 — API endpoints sugeridos (contratos mínimos)

* `POST /payments` — cria payment (body: JSON payload do boleto/exemplo).
* `GET /payments?pendingExpectations=true` — lista pagamentos com `expectativaCalculada = false`.
* `GET /expectations` — lista expectativas geradas.
* `GET /expectations/{id}` — detalhe.
* `POST /expectations/{id}/feedback` — aceito/rejeitado + comentários.
* `POST /admin/reprocess/{paymentId}` — reprocessa payment (idempotência requerida).

Exemplo payload `POST /payments`:

```json
{
  "identificationField": "836400000045837401380055515194495112081437529738",
  "value": 483.74,
  "originalValue": 483.74,
  "dueDate": null,
  "beneficiaryName": "CEMIG DISTRIBUICAO",
  "createdAt": "2025-10-01T10:00:00Z"
}
```

---

## 8 — Worker / fluxo de processamento (detalhado)

**ExpectationWorker (BackgroundService)** — loop simples:

1. Buscar batch (ex: 50) de `Payments` com `ExpectationCalculated == false`.
2. Para cada pagamento:

   * Aplicar **rule-based** (checagens rápidas: se `DueDate` presente -> expected = dueDate; se `value == 0` -> fallback, etc).
   * Montar contexto: payment + K itens mais recentes do mesmo beneficiário (RAG).
   * Chamar `ILlmClient.GetExpectationAsync(prompt, context)`.
   * Validar/parsear JSON retornado.
   * Salvar `Expectation` e atualizar `Payment.ExpectationCalculated = true`, `Payment.ExpectationId = expectation.Id`.
   * Gravar `AuditLog` com raw LLM response + metadata (model/version/temperature).
3. Commit do batch; tratar erros com retry/backoff e idempotência.

**Idempotência & Concurrency**

* Ao buscar batch, aplique um lock lógico (ex: `Processing = true` + `ProcessingAt`) ou use row versioning para evitar dois workers concorrentes processando o mesmo registro.
* Use transações curtas para gravar `Expectation` + atualizar `Payment`.

---

## 9 — ILlmClient — adapter para LLMs

**Contrato mínimo**

```csharp
public interface ILlmClient
{
    Task<string> GetExpectationAsync(string prompt, CancellationToken ct);
}
```

**Implementações**

* `FakeLlmClient` — retorna JSON fixo para testes.
* `OpenAiClient` — usa HttpClient para chamar OpenAI/Azure OpenAI.
* `LocalLlmClient` — se tiver um servidor local (Ollama/llama.cpp/ggml) expõe HTTP e responde.

**Boas práticas para o adapter**

* Timeout configurável.
* Retries com política exponencial (ex: Polly).
* Sanitize de entrada (remover PII quando usar LLMs externos).
* Log raw request/response (mas cuidado com dados sensíveis — grave criptografado se necessário).

---

## 10 — Prompt engineering (templates e validação)

**Template de prompt (retornar somente JSON)**

```
Você é um assistente que sugere uma expectativa de pagamento para uma fatura.
Retorne **apenas** um JSON válido com as chaves:
expectedPaymentDate (YYYY-MM-DD or null), expectedAmount (decimal), confidenceScore (0..1), rationale (string <= 120 chars).

Contexto do pagamento:
{PAYMENT_JSON}

Regras:
- Se dueDate for informado e válido, use ele como expectedPaymentDate, exceto se histórico indicar comportamento diferente.
- Se dueDate null, tente inferir com base no histórico (últimas 6 faturas do mesmo beneficiário): [HISTORICAL_SUMMARY]
- confidenceScore: estime grau de confiança na sugestão.
- rationale: explique em até 2 frases por que sugeriu isso.

Retorne apenas JSON.
```

**Validação da resposta**

* Use `System.Text.Json` para parsear.
* Schema-check: campos obrigatórios e tipos.
* Em caso de response inválida -> fallback rule-based + log do erro.

---

## 11 — RAG (retrieval-augmented generation) — básico para POC

* **Quando usar:** enriquecer prompts com histórico (ex.: comportamento histórico do pagador para aquele beneficiário).
* **Como (POC simples):**

  * Mantenha um índice simples no DB: últimas N faturas por beneficiário (data, valor, atraso).
  * Ao processar, monte um `HISTORICAL_SUMMARY` (text short) com padrões (ex.: "ultimas 6: 5 pagas pontualmente, 1 com atraso de 10 dias, média de pagamento = 3 dias antes do vencimento").
  * Inclua esse resumo no prompt em vez de anexar documentos vetoriais.
* **Evolução:** indexar documentos em vetor DB (Qdrant) e recuperar similaridade K + enviar snippets.

---

## 12 — Observability & métricas

* **Logs**: Serilog -> Seq. Grave raw LLM response com identificador (não exponha PII em logs não criptografados).
* **Metrics**: contadores (processedPayments, failedPredictions, avgLatency), histograma de confidenceScore.
* **Dashboard**: endpoint de admin exibindo:

  * Taxa de acerto vs feedback humano.
  * Distribuição confidenceScore.
  * Pagamentos pendentes.

---

## 13 — Segurança e conformidade

* **Chaves**: mantenha keys em Secrets Manager fora do commit.

---

## 14 — Docker-compose exemplo (mínimo)

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "Your_password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
  api:
    build: ./src/Api
    depends_on: [sqlserver]
    environment:
      - ConnectionStrings__Default=Server=sqlserver,1433;Database=gestor;User Id=sa;Password=Your_password123;
    ports:
      - "5000:80"
  worker:
    build: ./src/Worker
    depends_on: [api]
    environment:
      - ConnectionStrings__Default=Server=sqlserver,1433;Database=gestor;User Id=sa;Password=Your_password123;
```

---

## 15 — Sugestões de LLMs gratuitas / locais para POC

> Recomendação: comece com local pequeno para desenvolvimento e testes; depois integre cloud se precisar escala/qualidade.

**Locais / Open-source (RODAR LOCALMENTE)**

* **Llama 2 (7B)** via `llama.cpp` / `ggml` / `Ollama` — compatível para POC; boa latência em máquinas com GPU/CPU modernas.
* **Vicuna / Alpaca** — instrução-tuned; útil se quiser fine-tunar para respostas mais controladas.
* **Mistral (7B)** — eficientes para inferência; verificar disponibilidade e licença.
* Rodar localmente evita custos e risco de expor dados sensíveis.

**Hosted com camada gratuita / trial**

* **OpenAI** — créditos iniciais em novas contas; fácil integração via SDK.
* **Azure OpenAI** — quando exigir infra corporativa/segurança, mas costuma ser paga.
* **Anthropic** — planos pagos; testar com trial se disponível.

**Estratégia recomendada para POC**

1. **Dev**: `FakeLlmClient` -> `LocalLlmClient` (Ollama + Llama2 7B) — permite iteração rápida e offline.
2. **Staging**: teste com OpenAI (camada trial) para comparar qualidade e latência.
3. **Prod (futuro)**: selecionar provider com SLAs ou manter self-hosted (se regulatório/sensível).

---

## 17 — Passo-a-passo “getting started” (para colar no Cursor)

1. **Criar solution & projetos**

   ```bash
   dotnet new sln -n GestorExpectativas.IA.POC
   dotnet new webapi -n Api -f net8.0
   dotnet new console -n Worker -f net8.0
   dotnet new classlib -n Core -f net8.0
   dotnet new classlib -n Infrastructure -f net8.0
   dotnet sln add Api/Worker/Core/Infrastructure
   ```

2. **Instalar pacotes**

   ```bash
   # EF Core + SQL Server
   dotnet add Api package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add Worker package Microsoft.EntityFrameworkCore
   dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Design
   # Logging
   dotnet add Api package Serilog.AspNetCore
   # HttpClientFactory
   dotnet add Api package Microsoft.Extensions.Http
   ```

3. **Criar modelos (copiar Payment/Expectation/AuditLog)** — adicionar migrations:

   ```bash
   dotnet ef migrations add InitialCreate -p Infrastructure -s Api
   dotnet ef database update -p Infrastructure -s Api
   ```

4. **Implementar ILlmClient fake** — testar Worker com resposta fixa.

5. **Expor endpoints CRUD** e testar com Postman/insomnia.

6. **Rodar Worker** localmente; inserir `payments` e validar `expectations`.

7. **Substituir ILlmClient fake por LocalLlmClient** (Ollama ou servidor local) ou `OpenAiClient`.

8. **Adicionar logs e endpoint de feedback**.

9. **Criar docker-compose** e testar infra completa.

---

## 18 — Boas práticas / recomendações finais

* **Comece com regras claras** e um LLM como “advisor” (explicável), evolua para ML só quando houver dados suficientes para treinar.
* **Persistência de meta**: registre qual modelo/versão foi usado para cada expectativa.
* **Trate LLM como probabilístico**: sempre registre confidenceScore e permita revisão manual.
* **Proteja PII**: se usar LLMs externos, mascarar/anonimizar dados sensíveis.
* **Idempotência** e locking: imprescindíveis para evitar duplicidade.

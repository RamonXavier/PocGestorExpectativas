# 📊 Respostas Esperadas - Testes de IA

## 🧪 **TESTE 1: Primeiro CEMIG (Sem Histórico)**

### Request:
```json
POST /api/test/test-ai
{
  "beneficiaryName": "CEMIG DISTRIBUICAO S.A.",
  "value": 483.74
}
```

### Response Esperada:
```json
{
  "message": "Análise de IA concluída",
  "paymentId": "guid-gerado"
}
```

### Expectativa Gerada:
```json
{
  "id": "guid",
  "normalizedBeneficiary": "CEMIG",
  "nextExpectedPaymentDate": "2025-11-20", // ~30 dias
  "nextExpectedAmount": 483.74,
  "confidenceScore": 0.3,
  "rationale": "Histórico insuficiente (1 registro). Estimativa baseada no último pagamento.",
  "analysisMethod": "rule-based",
  "historyCount": 1
}
```

---

## 🧪 **TESTE 2: Segundo CEMIG (RAG Funcionando)**

### Request:
```json
POST /api/test/test-ai
{
  "beneficiaryName": "Cemig Energia Ltda",
  "value": 485.20
}
```

### Expectativa Gerada:
```json
{
  "id": "guid",
  "normalizedBeneficiary": "CEMIG", // ← MESMO GRUPO!
  "nextExpectedPaymentDate": "2025-11-21",
  "nextExpectedAmount": 486.50,
  "confidenceScore": 0.7,
  "rationale": "Baseado em 2 pagamentos históricos. Padrão de crescimento mensal identificado.",
  "analysisMethod": "llm",
  "historyCount": 2
}
```

---

## 🧪 **TESTE 3: Terceiro CEMIG (IA Avançada)**

### Request:
```json
POST /api/test/test-ai
{
  "beneficiaryName": "CEMIG DISTRIBUICAO",
  "value": 487.50
}
```

### Expectativa Gerada:
```json
{
  "id": "guid",
  "normalizedBeneficiary": "CEMIG",
  "nextExpectedPaymentDate": "2025-11-22",
  "nextExpectedAmount": 489.25,
  "confidenceScore": 0.85,
  "rationale": "Baseado em 3 pagamentos históricos. Padrão mensal consistente com crescimento de 0.5% ao mês. Alta previsibilidade.",
  "analysisMethod": "llm",
  "historyCount": 3
}
```

---

## 🧪 **TESTE 4: COPASA (Novo Beneficiário)**

### Request:
```json
POST /api/test/test-ai
{
  "beneficiaryName": "COPASA MG SANEAMENTO",
  "value": 89.90
}
```

### Expectativa Gerada:
```json
{
  "id": "guid",
  "normalizedBeneficiary": "COPASA",
  "nextExpectedPaymentDate": "2025-11-20",
  "nextExpectedAmount": 89.90,
  "confidenceScore": 0.3,
  "rationale": "Histórico insuficiente (1 registro). Estimativa baseada no último pagamento.",
  "analysisMethod": "rule-based",
  "historyCount": 1
}
```

---

## 📊 **ADMIN STATS - Estado Inicial**

### Response:
```json
{
  "payments": {
    "total": 0,
    "paid": 0,
    "unpaid": 0,
    "averageValue": 0,
    "recentPayments": 0
  },
  "expectations": {
    "total": 0,
    "averageConfidence": 0
  },
  "audit": {
    "totalLogs": 0
  },
  "system": {
    "databaseConnected": true,
    "timestamp": "2025-10-21T..."
  }
}
```

---

## 📊 **ADMIN STATS - Após Testes**

### Response:
```json
{
  "payments": {
    "total": 4,
    "paid": 4,
    "unpaid": 0,
    "averageValue": 361.59, // (483.74 + 485.20 + 487.50 + 89.90) / 4
    "recentPayments": 4
  },
  "expectations": {
    "total": 4,
    "averageConfidence": 0.58 // (0.3 + 0.7 + 0.85 + 0.3) / 4
  },
  "audit": {
    "totalLogs": 8 // 4 payments + 4 expectations
  },
  "system": {
    "databaseConnected": true,
    "timestamp": "2025-10-21T..."
  }
}
```

---

## 📈 **BENEFICIARY STATS - Após Testes**

### Response:
```json
[
  {
    "beneficiary": "CEMIG",
    "paymentCount": 3,
    "totalValue": 1456.44,
    "averageValue": 485.48,
    "lastPayment": "2025-10-21T...",
    "hasExpectation": true
  },
  {
    "beneficiary": "COPASA",
    "paymentCount": 1,
    "totalValue": 89.90,
    "averageValue": 89.90,
    "lastPayment": "2025-10-21T...",
    "hasExpectation": true
  }
]
```

---

## 📋 **AUDIT LOGS - Exemplos**

### Response:
```json
[
  {
    "id": "guid",
    "paymentId": "guid",
    "expectationId": "guid",
    "action": "expectation_generated",
    "details": "IA analisou 3 pagamentos históricos. Confiança: 85%",
    "timestamp": "2025-10-21T..."
  },
  {
    "id": "guid",
    "paymentId": "guid",
    "expectationId": null,
    "action": "payment_received",
    "details": "Pagamento processado via RabbitMQ: CEMIG DISTRIBUICAO - R$ 487,50",
    "timestamp": "2025-10-21T..."
  }
]
```

---

## 🐰 **RABBITMQ TESTS - Batch Data**

### Response:
```json
{
  "message": "Dados de teste enviados",
  "results": [
    {
      "success": true,
      "beneficiary": "COPASA MG"
    },
    {
      "success": true,
      "beneficiary": "CEMIG DISTRIBUICAO"
    },
    {
      "success": true,
      "beneficiary": "SABESP"
    }
  ]
}
```

---

## 🔮 **EXPECTATIONS STATS**

### Response:
```json
{
  "totalExpectations": 6,
  "averageConfidence": 0.62,
  "uniqueBeneficiaries": 3
}
```

---

## ⚠️ **POSSÍVEIS ERROS**

### **Groq API Error:**
```json
{
  "error": {
    "message": "The model `llama-3.1-70b-versatile` has been decommissioned...",
    "type": "invalid_request_error",
    "code": "model_decommissioned"
  }
}
```
**Solução:** Atualizar modelo para `llama-3.3-70b-versatile`

### **RabbitMQ Connection Error:**
```json
{
  "error": "No connection could be made because the target machine actively refused it."
}
```
**Solução:** Verificar configurações RabbitMQ

### **Database Error:**
```json
{
  "error": "A network-related or instance-specific error occurred..."
}
```
**Solução:** Verificar connection string e executar migrations

---

## 🎯 **INDICADORES DE SUCESSO**

### **✅ IA Funcionando Corretamente:**
- Confidence Score cresce com histórico (0.3 → 0.7 → 0.8+)
- RAG agrupa beneficiários similares
- Rationale fica mais detalhada
- Analysis Method evolui: rule-based → llm

### **✅ Sistema Completo:**
- RabbitMQ processa mensagens
- Worker consome automaticamente
- Expectativas são geradas
- Audit logs registram tudo
- Admin stats mostram crescimento

### **✅ Qualidade da IA:**
- Normalização consistente de nomes
- Padrões identificados corretamente
- Explicações coerentes (rationale)
- Confidence apropriada para contexto

---

**💡 Use estes exemplos para validar se sua implementação está funcionando corretamente!**

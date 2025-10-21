# 🚀 Guia da Collection Postman - Gestor de Expectativas

## 📥 **COMO IMPORTAR**

1. **Abra o Postman**
2. **Clique em "Import"** (botão no canto superior esquerdo)
3. **Selecione "Upload Files"**
4. **Escolha o arquivo:** `GestorExpectativas-Postman-Collection.json`
5. **Clique "Import"**

## ⚙️ **CONFIGURAÇÃO INICIAL**

### **1. Configurar Variável de Ambiente**
1. Na collection importada, clique na aba **"Variables"**
2. Ajuste a variável `baseUrl`:
   - **HTTPS**: `https://localhost:7000` (porta padrão HTTPS)
   - **HTTP**: `http://localhost:5000` (porta padrão HTTP)
3. **Salve** a collection (Ctrl+S)

### **2. Verificar se API está rodando**
```bash
# No terminal do projeto
dotnet run

# Verifique a porta no output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:7000
```

---

## 🧪 **FLUXO DE TESTES RECOMENDADO**

### **🎯 FASE 1: Verificação Inicial**
1. **📊 Admin Stats** - Ver estado inicial do sistema
2. **🐰 Queue Status** - Verificar RabbitMQ
3. **📋 Audit Logs** - Ver logs iniciais

### **🎯 FASE 2: Testes de IA (PRINCIPAL)**
Execute na ordem para ver evolução da IA:

1. **🤖 Teste IA - Primeiro CEMIG**
   - Resultado: Expectativa básica (sem histórico)
   - Confidence: ~0.3

2. **🤖 Teste IA - Segundo CEMIG (Variação)**
   - Resultado: IA agrupa nomes diferentes
   - Confidence: ~0.7

3. **🤖 Teste IA - Terceiro CEMIG**
   - Resultado: Análise avançada com padrões
   - Confidence: ~0.8-0.9

4. **📊 Ver Expectativas Geradas**
   - Analise a evolução da qualidade

5. **🤖 Teste IA - COPASA (Novo Beneficiário)**
   - Resultado: Novo beneficiário, expectativa básica

### **🎯 FASE 3: Testes RabbitMQ**
1. **📦 Criar Dados de Teste (Batch)**
   - Envia 3 pagamentos via fila
   - Aguarde 5-10 segundos para processamento

2. **📤 Enviar Pagamento Individual**
   - Teste fluxo completo individual

3. **📊 Ver Expectativas Geradas**
   - Verifique novos resultados

### **🎯 FASE 4: Análise de Resultados**
1. **📊 Admin Stats** - Compare com estado inicial
2. **📈 Estatísticas por Beneficiário** - Veja padrões
3. **📋 Audit Logs** - Rastreie todas operações

---

## 🔍 **O QUE OBSERVAR NOS TESTES**

### **🧠 Evolução da IA:**
- **Normalização**: Nomes similares → mesmo grupo
- **Confidence Score**: Cresce com mais histórico
- **Rationale**: Explicações mais detalhadas
- **Analysis Method**: "rule-based" → "llm"

### **📊 Métricas Importantes:**
- **History Count**: Quantos pagamentos analisados
- **Average Confidence**: Qualidade geral da IA
- **Unique Beneficiaries**: Diversidade de dados

### **🔄 Fluxo RabbitMQ:**
- **Envio**: API → RabbitMQ
- **Processamento**: Worker → IA → Banco
- **Auditoria**: Logs completos

---

## 🎯 **CENÁRIOS DE TESTE ESPECÍFICOS**

### **Cenário 1: RAG (Normalização)**
```
Teste: Nomes diferentes, mesmo beneficiário
- "CEMIG DISTRIBUICAO S.A."
- "Cemig Energia Ltda"  
- "CEMIG DISTRIBUICAO"

Resultado Esperado: Todos → "CEMIG"
```

### **Cenário 2: Evolução da Confiança**
```
CEMIG Pagamento 1: Confidence ~0.3 (sem histórico)
CEMIG Pagamento 2: Confidence ~0.7 (2 registros)
CEMIG Pagamento 3: Confidence ~0.8+ (padrão identificado)
```

### **Cenário 3: Beneficiários Diversos**
```
- CEMIG: Energia elétrica
- COPASA: Saneamento  
- SABESP: Saneamento
- ENEL: Energia elétrica

Resultado: IA identifica padrões por tipo de serviço
```

---

## 🚨 **TROUBLESHOOTING**

### **❌ Erro 404 - Not Found**
- ✅ Verifique se API está rodando: `dotnet run`
- ✅ Confirme a porta na variável `baseUrl`
- ✅ Teste no navegador: `https://localhost:7000/swagger`

### **❌ Erro 500 - Internal Server Error**
- ✅ Verifique logs no terminal da API
- ✅ Confirme se banco está configurado
- ✅ Teste conexão com: `dotnet ef database update`

### **❌ Groq API Error**
- ✅ Verifique API Key no `appsettings.Development.json`
- ✅ Confirme modelo: `llama-3.3-70b-versatile`
- ✅ Teste conectividade: https://console.groq.com/

### **❌ RabbitMQ Connection Error**
- ✅ Verifique configurações no `appsettings.Development.json`
- ✅ Confirme credenciais do CloudAMQP
- ✅ Teste conexão manual

---

## 📊 **INTERPRETANDO RESULTADOS**

### **Expectation Response Example:**
```json
{
  "id": "guid",
  "normalizedBeneficiary": "CEMIG",
  "nextExpectedPaymentDate": "2025-11-20",
  "nextExpectedAmount": 489.25,
  "confidenceScore": 0.85,
  "rationale": "Baseado em 3 pagamentos históricos. Padrão mensal com crescimento de 0.5% ao mês.",
  "analysisMethod": "llm",
  "historyCount": 3,
  "createdAt": "2025-10-21T..."
}
```

### **Campos Importantes:**
- **confidenceScore**: 0-1 (quanto maior, melhor)
- **rationale**: Explicação da IA
- **analysisMethod**: Como foi gerada
- **historyCount**: Dados utilizados

---

## 🎯 **PRÓXIMOS PASSOS**

Após testar a collection:

1. **📈 Analise Padrões**: Veja como IA melhora com dados
2. **🔧 Ajuste Prompts**: Modifique templates no código
3. **📊 Monitore Métricas**: Use endpoints de admin
4. **🚀 Escale Testes**: Adicione mais beneficiários
5. **🤖 Compare Modelos**: Teste diferentes LLMs

---

## 💡 **DICAS AVANÇADAS**

### **Variáveis Dinâmicas:**
- Copie IDs dos responses para `paymentId` e `expectationId`
- Use para testes de CRUD específicos

### **Testes em Sequência:**
- Execute requests em ordem para ver evolução
- Aguarde processamento assíncrono (RabbitMQ)

### **Monitoramento:**
- Use Admin endpoints para acompanhar sistema
- Verifique logs de auditoria regularmente

---

**🚀 Agora você tem tudo para testar seu sistema de IA completo!**

A collection está organizada por funcionalidade e inclui exemplos realistas para demonstrar o poder da IA na análise de padrões de pagamento.

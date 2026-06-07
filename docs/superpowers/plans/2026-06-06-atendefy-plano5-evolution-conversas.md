# Plano 5 — Evolution API + Conversas

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrar a Evolution API localmente para receber mensagens WhatsApp reais, e implementar a tela de Conversas no frontend com listagem e histórico de mensagens.

**Architecture:** A Evolution API roda como container Docker na rede `atendefy`; recebe mensagens do WhatsApp e chama o webhook `POST /webhooks/evolution?token={accountId}` da API. O backend já persiste conversas via `ConversationWorker` → `ConversationService.PersistAsync`. Dois novos endpoints expõem os dados (`GET /conversations`, `GET /conversations/{id}/messages`). A `ConversationsPage` consome esses endpoints com React Query.

**Tech Stack:** .NET 8 Minimal APIs, EF Core 8 (Npgsql), Docker Compose override, React 18, React Query v5, Tailwind CSS, shadcn/ui

---

## Mapa de Arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `infra/docker-compose.override.yml` | Modificar | Adicionar serviço `evolution-api` sem profile de produção |
| `src/Atendefy.Web/src/pages/WhatsAppPage.tsx` | Modificar | Corrigir placeholder do configJson: `baseUrl`→`base_url`, `apiKey`→`api_key` |
| `src/Atendefy.API/Modules/Chatbot/ConversationEndpoints.cs` | Criar | `GET /conversations` (paginado) e `GET /conversations/{id}/messages` |
| `src/Atendefy.API/Program.cs` | Modificar | Registrar `app.MapConversationEndpoints()` |
| `src/Atendefy.Web/src/types/api.ts` | Modificar | Adicionar tipos `ConversationSummary`, `ConversationDetail`, `ConversationMessage` |
| `src/Atendefy.Web/src/hooks/useConversations.ts` | Criar | Hooks React Query para conversas e mensagens |
| `src/Atendefy.Web/src/pages/ConversationsPage.tsx` | Modificar | Implementação completa: lista + histórico de mensagens |

---

## Contexto do Fluxo de Mensagens (necessário para entender o plano)

```
WhatsApp → Evolution API → POST /webhooks/evolution?token={accountId_sem_traços}
  → EvolutionWebhookValidator (resolve token → tenant)
  → RedisStreamService (publica em "messages.inbound")
  → ConversationWorker (consome stream)
  → AIProvider (gera resposta)
  → EvolutionProvider (envia reply via Evolution API)
  → ConversationService.PersistAsync (salva em tenant DB: conversations + messages)
```

O `token` na URL do webhook é o UUID da conta WhatsApp **sem traços** (`account.Id.ToString("N")`).

---

## Seção A — Evolution API Local

### Task 1: Adicionar Evolution API ao docker-compose.override.yml

**Files:**
- Modify: `infra/docker-compose.override.yml`

- [ ] **Step 1: Editar docker-compose.override.yml**

Adicionar o serviço `evolution-api` ao final do arquivo. O tag `!reset` remove o `profiles: [production]` definido no `docker-compose.yml` base (requer Docker Compose ≥ 2.20):

```yaml
services:
  postgres:
    ports:
      - "5432:5432"

  redis:
    ports:
      - "6379:6379"

  atendefy-api:
    build:
      context: ..
      dockerfile: src/Atendefy.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    ports:
      - "8080:8080"
    volumes:
      - ../logs:/app/logs

  atendefy-web:
    build:
      context: ../src/Atendefy.Web
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      - atendefy-api

  evolution-api:
    profiles: !reset []
    ports:
      - "8081:8080"
    environment:
      - SERVER_URL=http://localhost:8081
      - AUTHENTICATION_API_KEY=dev_evolution_key
      - DATABASE_ENABLED=false
      - CACHE_REDIS_ENABLED=false
      - CACHE_LOCAL_ENABLED=true
```

> **Fallback** se `!reset` não funcionar (Docker Compose < 2.20): iniciar manualmente com `docker compose --profile production up evolution-api -d` ao invés de incluir no override.

- [ ] **Step 2: Subir Evolution API e verificar**

```powershell
cd infra
docker compose up -d evolution-api
```

Aguardar ~10 segundos e verificar que está saudável:

```powershell
curl http://localhost:8081/
```

Resposta esperada: JSON com status/version da Evolution API (qualquer 2xx).

- [ ] **Step 3: Criar uma instância na Evolution API**

```powershell
curl -X POST http://localhost:8081/instance/create `
  -H "Content-Type: application/json" `
  -H "apikey: dev_evolution_key" `
  -d '{"instanceName":"atendefy-dev","qrcode":false}'
```

Resposta esperada: `{"instance":{"instanceName":"atendefy-dev",...}}` — anote o nome da instância.

- [ ] **Step 4: Commit**

```bash
git add infra/docker-compose.override.yml
git commit -m "feat: add evolution-api service to local docker-compose"
```

---

### Task 2: Corrigir bug no placeholder do configJson (camelCase → snake_case)

**Context:** `EvolutionConfig.FromJson` em `WhatsAppConfigJson.cs` lê `base_url` e `api_key` (snake_case). Mas o placeholder no frontend envia `baseUrl` e `apiKey` (camelCase), causando `KeyNotFoundException` quando o `ConversationWorker` tenta criar o `EvolutionProvider`.

**Files:**
- Modify: `src/Atendefy.Web/src/pages/WhatsAppPage.tsx:29-33`

- [ ] **Step 1: Corrigir o placeholder do Evolution no WhatsAppPage.tsx**

Substituir as linhas 29–33 do arquivo `src/Atendefy.Web/src/pages/WhatsAppPage.tsx`:

**Antes:**
```typescript
  evolution: JSON.stringify(
    { baseUrl: 'http://evolution-api:8080', instance: 'my-instance', apiKey: 'your-api-key' },
    null,
    2
  ),
```

**Depois:**
```typescript
  evolution: JSON.stringify(
    { base_url: 'http://evolution-api:8080', instance: 'atendefy-dev', api_key: 'dev_evolution_key' },
    null,
    2
  ),
```

> **Atenção:** contas existentes com configJson em camelCase precisam ser recriadas. Em dev isso é normal — deletar a conta no banco e criar novamente.

- [ ] **Step 2: Reconstruir o container web**

```powershell
cd infra
docker compose up -d --build atendefy-web
```

- [ ] **Step 3: Verificar no browser**

1. Abrir `http://localhost:3000`
2. Navegar para Contas WhatsApp → Nova conta
3. Selecionar "Evolution API"
4. Confirmar que o placeholder agora mostra `base_url`, `instance`, `api_key`

- [ ] **Step 4: Commit**

```bash
git add src/Atendefy.Web/src/pages/WhatsAppPage.tsx
git commit -m "fix: evolution configJson placeholder uses snake_case keys to match EvolutionConfig.FromJson"
```

---

## Seção B — Backend: Endpoints de Conversas

### Task 3: Criar ConversationEndpoints.cs

**Files:**
- Create: `src/Atendefy.API/Modules/Chatbot/ConversationEndpoints.cs`

**Context:** `TenantDbContext` tem DbSets `Conversations` (`Conversation`) e `Messages` (`ConversationMessage`). O `TenantDbContextFactory` é singleton; `PublicDbContext` é scoped. O padrão de endpoints segue `WhatsAppEndpoints.cs`: extrair `tenant_id` do JWT, buscar `SchemaName` no `PublicDbContext`, criar contexto do tenant com `dbFactory.Create(schemaName)`.

- [ ] **Step 1: Criar o arquivo**

Criar `src/Atendefy.API/Modules/Chatbot/ConversationEndpoints.cs` com o conteúdo:

```csharp
using Atendefy.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atendefy.API.Modules.Chatbot;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Conversations")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            TenantDbContextFactory dbFactory,
            PublicDbContext publicDb,
            HttpContext ctx) =>
        {
            var (schemaName, error) = await ResolveSchemaAsync(ctx, publicDb);
            if (error is not null) return Results.Json(new { error }, statusCode: 401);

            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            await using var db = dbFactory.Create(schemaName);

            var conversations = await db.Conversations
                .Where(c => !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.ContactPhone,
                    c.MessageCount,
                    c.StartedAt,
                    LastMessageAt = c.Messages.Max(m => (DateTime?)m.CreatedAt) ?? c.StartedAt
                })
                .OrderByDescending(c => c.LastMessageAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await db.Conversations.CountAsync(c => !c.IsDeleted);

            return Results.Ok(new { conversations, total, page, pageSize });
        });

        group.MapGet("/{id:guid}/messages", async (
            Guid id,
            TenantDbContextFactory dbFactory,
            PublicDbContext publicDb,
            HttpContext ctx) =>
        {
            var (schemaName, error) = await ResolveSchemaAsync(ctx, publicDb);
            if (error is not null) return Results.Json(new { error }, statusCode: 401);

            await using var db = dbFactory.Create(schemaName);

            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (conversation is null) return Results.NotFound();

            var messages = await db.Messages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.Id, m.Role, m.Content, m.TokensUsed, m.CreatedAt })
                .ToListAsync();

            return Results.Ok(new
            {
                conversation.Id,
                conversation.ContactPhone,
                conversation.StartedAt,
                conversation.MessageCount,
                messages
            });
        });

        return app;
    }

    private static async Task<(string SchemaName, string? Error)> ResolveSchemaAsync(
        HttpContext ctx, PublicDbContext publicDb)
    {
        var tenantIdStr = ctx.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
            return (string.Empty, "Token inválido");

        var tenant = await publicDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null) return (string.Empty, "Tenant não encontrado");

        return (tenant.SchemaName, null);
    }
}
```

- [ ] **Step 2: Verificar que compila**

```powershell
cd src/Atendefy.API
dotnet build
```

Esperado: `Build succeeded` sem erros.

- [ ] **Step 3: Commit**

```bash
git add src/Atendefy.API/Modules/Chatbot/ConversationEndpoints.cs
git commit -m "feat: add GET /conversations and GET /conversations/{id}/messages endpoints"
```

---

### Task 4: Registrar ConversationEndpoints no Program.cs

**Files:**
- Modify: `src/Atendefy.API/Program.cs:163`

- [ ] **Step 1: Adicionar a chamada no Program.cs**

No `Program.cs`, localizar o bloco de mapeamento de endpoints (linhas 157–163). Adicionar `app.MapConversationEndpoints()` após `app.MapBillingWebhookEndpoints()`:

**Antes (linhas 157–163):**
```csharp
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapWhatsAppEndpoints();
app.MapAIEndpoints();
app.MapWebhookEndpoints();
app.MapBillingEndpoints();
app.MapBillingWebhookEndpoints();
```

**Depois:**
```csharp
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapWhatsAppEndpoints();
app.MapAIEndpoints();
app.MapWebhookEndpoints();
app.MapBillingEndpoints();
app.MapBillingWebhookEndpoints();
app.MapConversationEndpoints();
```

- [ ] **Step 2: Adicionar using no topo do Program.cs (se necessário)**

Verificar se há `using Atendefy.API.Modules.Chatbot;` no Program.cs. Se não houver, adicionar junto aos demais usings.

> **Nota:** O namespace já tem `using Atendefy.API.Modules.Chatbot;` por causa do `ConversationService` e `ConversationWorker`. Se o arquivo usar `global usings` ou `implicit usings`, pode não precisar.

- [ ] **Step 3: Build + rebuild container**

```powershell
cd src/Atendefy.API
dotnet build
```

```powershell
cd ../../infra
docker compose up -d --build atendefy-api
```

- [ ] **Step 4: Testar endpoints via curl**

Primeiro fazer login para obter token:

```powershell
$login = curl -s -X POST http://localhost:8080/auth/login `
  -H "Content-Type: application/json" `
  -d '{"email":"seuemail@teste.com","password":"SuaSenha123!"}' | ConvertFrom-Json

$token = $login.accessToken
```

Testar lista de conversas:

```powershell
curl -H "Authorization: Bearer $token" http://localhost:8080/conversations?page=1&pageSize=20
```

Resposta esperada: `{"conversations":[],"total":0,"page":1,"pageSize":20}` (lista vazia é normal antes de simular mensagens).

- [ ] **Step 5: Commit**

```bash
git add src/Atendefy.API/Program.cs
git commit -m "feat: register conversation endpoints in Program.cs"
```

---

## Seção C — Frontend: Tela de Conversas

### Task 5: Adicionar tipos TypeScript para conversas

**Files:**
- Modify: `src/Atendefy.Web/src/types/api.ts`

- [ ] **Step 1: Adicionar tipos ao final de api.ts**

Abrir `src/Atendefy.Web/src/types/api.ts` e adicionar ao final:

```typescript
// Conversations
export interface ConversationSummary {
  id: string;
  contactPhone: string;
  messageCount: number;
  startedAt: string;
  lastMessageAt: string;
}

export interface ConversationMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  tokensUsed: number;
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  contactPhone: string;
  startedAt: string;
  messageCount: number;
  messages: ConversationMessage[];
}

export interface ConversationsListResponse {
  conversations: ConversationSummary[];
  total: number;
  page: number;
  pageSize: number;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Atendefy.Web/src/types/api.ts
git commit -m "feat: add TypeScript types for conversations API"
```

---

### Task 6: Criar useConversations hook

**Files:**
- Create: `src/Atendefy.Web/src/hooks/useConversations.ts`

- [ ] **Step 1: Criar o arquivo**

Criar `src/Atendefy.Web/src/hooks/useConversations.ts`:

```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type { ConversationsListResponse, ConversationDetail } from '@/types/api';

export function useConversations(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['conversations', page, pageSize],
    queryFn: () =>
      apiClient
        .get<ConversationsListResponse>('/conversations', { params: { page, pageSize } })
        .then((r) => r.data),
  });
}

export function useConversationMessages(id: string | null) {
  return useQuery({
    queryKey: ['conversations', id, 'messages'],
    queryFn: () =>
      apiClient
        .get<ConversationDetail>(`/conversations/${id}/messages`)
        .then((r) => r.data),
    enabled: !!id,
  });
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Atendefy.Web/src/hooks/useConversations.ts
git commit -m "feat: add useConversations and useConversationMessages hooks"
```

---

### Task 7: Implementar ConversationsPage

**Files:**
- Modify: `src/Atendefy.Web/src/pages/ConversationsPage.tsx`

**Design:** Layout two-panel — coluna esquerda (lista de conversas, 320px) + coluna direita (histórico de mensagens, flex-grow). Mensagens do usuário alinhadas à esquerda, do assistente à direita (bolhas de chat).

- [ ] **Step 1: Substituir o conteúdo de ConversationsPage.tsx**

```tsx
import { useState } from 'react';
import { MessageSquare, Phone } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useConversations, useConversationMessages } from '@/hooks/useConversations';

function formatTime(dateStr: string): string {
  const d = new Date(dateStr);
  const now = new Date();
  const isToday = d.toDateString() === now.toDateString();
  if (isToday) return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
}

export default function ConversationsPage() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const { data, isLoading } = useConversations();
  const { data: detail, isLoading: loadingMessages } = useConversationMessages(selectedId);

  return (
    <div className="flex h-[calc(100vh-8rem)] gap-4">
      {/* Painel esquerdo: lista de conversas */}
      <div className="w-80 shrink-0 flex flex-col border rounded-lg overflow-hidden bg-card">
        <div className="p-4 border-b">
          <h1 className="text-lg font-semibold">Conversas</h1>
          {data && (
            <p className="text-xs text-muted-foreground mt-0.5">{data.total} contato(s)</p>
          )}
        </div>

        <div className="flex-1 overflow-y-auto">
          {isLoading && (
            <p className="p-4 text-sm text-muted-foreground">Carregando...</p>
          )}

          {!isLoading && data?.conversations.length === 0 && (
            <div className="p-6 text-center text-sm text-muted-foreground">
              <MessageSquare className="h-8 w-8 mx-auto mb-2 opacity-30" />
              <p>Nenhuma conversa ainda.</p>
              <p className="mt-1 text-xs">
                Envie uma mensagem via WhatsApp para começar.
              </p>
            </div>
          )}

          {data?.conversations.map((conv) => (
            <button
              key={conv.id}
              type="button"
              className={cn(
                'w-full text-left px-4 py-3 border-b hover:bg-accent transition-colors',
                selectedId === conv.id && 'bg-accent'
              )}
              onClick={() => setSelectedId(conv.id)}
            >
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium truncate">{conv.contactPhone}</span>
                <span className="text-xs text-muted-foreground shrink-0 ml-2">
                  {formatTime(conv.lastMessageAt)}
                </span>
              </div>
              <Badge variant="outline" className="text-xs py-0 h-5">
                {conv.messageCount} msgs
              </Badge>
            </button>
          ))}
        </div>
      </div>

      {/* Painel direito: mensagens */}
      <div className="flex-1 flex flex-col border rounded-lg overflow-hidden bg-card">
        {!selectedId ? (
          <div className="flex-1 flex items-center justify-center text-muted-foreground">
            <div className="text-center">
              <MessageSquare className="h-12 w-12 mx-auto mb-3 opacity-20" />
              <p className="text-sm">Selecione uma conversa</p>
            </div>
          </div>
        ) : (
          <>
            <div className="px-4 py-3 border-b flex items-center gap-2 shrink-0">
              <Phone className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium text-sm">{detail?.contactPhone ?? '…'}</span>
              {detail && (
                <span className="text-xs text-muted-foreground ml-auto">
                  desde {new Date(detail.startedAt).toLocaleDateString('pt-BR')}
                </span>
              )}
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-3">
              {loadingMessages && (
                <p className="text-sm text-center text-muted-foreground py-4">
                  Carregando mensagens…
                </p>
              )}

              {detail?.messages.map((msg) => (
                <div
                  key={msg.id}
                  className={cn('flex', msg.role === 'user' ? 'justify-start' : 'justify-end')}
                >
                  <div
                    className={cn(
                      'max-w-[75%] rounded-2xl px-4 py-2 text-sm',
                      msg.role === 'user'
                        ? 'bg-muted rounded-tl-sm'
                        : 'bg-primary text-primary-foreground rounded-tr-sm'
                    )}
                  >
                    <p className="whitespace-pre-wrap break-words">{msg.content}</p>
                    <p
                      className={cn(
                        'text-xs mt-1',
                        msg.role === 'user'
                          ? 'text-muted-foreground'
                          : 'text-primary-foreground/70'
                      )}
                    >
                      {formatTime(msg.createdAt)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Rebuild o container web**

```powershell
cd infra
docker compose up -d --build atendefy-web
```

- [ ] **Step 3: Verificar no browser (lista vazia)**

Abrir `http://localhost:3000`, navegar para "Conversas". Deve mostrar o layout dois painéis e a mensagem "Nenhuma conversa ainda." no painel esquerdo.

- [ ] **Step 4: Commit**

```bash
git add src/Atendefy.Web/src/pages/ConversationsPage.tsx
git commit -m "feat: implement ConversationsPage with two-panel layout and message history"
```

---

## Seção D — Smoke Test End-to-End

### Task 8: Criar conta WhatsApp, configurar webhook e simular mensagem

**Context:** Este task não requer código. Testa o fluxo completo:
`webhook → Redis → ConversationWorker → AI → persist → frontend mostra`.

**Pré-requisitos:**
- Todos os containers rodando: `docker compose up -d`
- Evolution API rodando: `docker compose up -d evolution-api`
- AI Config criada no tenant (provider=openai ou anthropic, com API key válida)
- Conta WhatsApp ainda **não** criada com o configJson correto — criar agora

- [ ] **Step 1: Criar conta WhatsApp com configJson correto**

1. Abrir `http://localhost:3000` → Contas WhatsApp → Nova conta
2. Selecionar "Evolution API"
3. Preencher:
   - Número: `+5511999999999` (número de teste)
   - Configuração JSON: confirmar que mostra `base_url`, `instance`, `api_key`
4. Clicar Salvar
5. **Anotar o `id` da conta** retornado (aparece na lista de contas) — ex: `550e8400-e29b-41d4-a716-446655440000`

- [ ] **Step 2: Obter o token do webhook (accountId sem traços)**

Converter o UUID da conta para o formato sem traços:

```powershell
$accountId = "550e8400-e29b-41d4-a716-446655440000"   # substituir pelo ID real
$token = $accountId -replace "-", ""
Write-Host "Token do webhook: $token"
# Saída: 550e8400e29b41d4a716446655440000
```

- [ ] **Step 3: Configurar webhook na instância Evolution (opcional — para uso real)**

Se quiser conectar um celular real, configurar o webhook na instância Evolution para chamar a API:

```powershell
curl -X POST "http://localhost:8081/webhook/set/atendefy-dev" `
  -H "Content-Type: application/json" `
  -H "apikey: dev_evolution_key" `
  -d "{`"url`":`"http://atendefy-api:8080/webhooks/evolution?token=$token`",`"enabled`":true,`"events`":[`"MESSAGES_UPSERT`"]}"
```

Para conectar o WhatsApp: `curl http://localhost:8081/instance/connect/atendefy-dev -H "apikey: dev_evolution_key"` → escanear QR code.

- [ ] **Step 4: Simular mensagem recebida via webhook (teste sem celular)**

```powershell
$token = "550e8400e29b41d4a716446655440000"   # substituir pelo token real

curl -X POST "http://localhost:8080/webhooks/evolution?token=$token" `
  -H "Content-Type: application/json" `
  -d '{"event":"messages.upsert","data":{"key":{"fromMe":false,"remoteJid":"5511987654321@s.whatsapp.net"},"message":{"conversation":"Olá! Preciso de ajuda com meu pedido."}}}'
```

Resposta esperada: `200 OK` ou `204 No Content`.

- [ ] **Step 5: Aguardar processamento e verificar logs**

```powershell
cd infra
docker compose logs atendefy-api --tail=30
```

Procurar por linhas indicando:
- `Inbound message received from 5511987654321`
- `AI response generated`
- `Message persisted`

Se AI Config não estiver configurada, o ConversationWorker loggará erro de AI e **não persistirá** — nesse caso, configurar AI Config antes de repetir.

- [ ] **Step 6: Verificar na tela de Conversas**

1. Abrir `http://localhost:3000` → Conversas
2. Deve aparecer `5511987654321@s.whatsapp.net` (ou número formatado) no painel esquerdo
3. Clicar na conversa → painel direito mostra a mensagem do usuário e a resposta do assistente

- [ ] **Step 7: Testar paginação via curl**

```powershell
$login = curl -s -X POST http://localhost:8080/auth/login `
  -H "Content-Type: application/json" `
  -d '{"email":"seuemail@teste.com","password":"SuaSenha123!"}' | ConvertFrom-Json

$token = $login.accessToken

curl -s -H "Authorization: Bearer $token" `
  "http://localhost:8080/conversations?page=1&pageSize=10" | ConvertFrom-Json | ConvertTo-Json
```

Esperado: `total: 1`, `conversations` com um item.

```powershell
# Buscar mensagens da conversa
$convId = "<id-da-conversa-do-step-anterior>"
curl -s -H "Authorization: Bearer $token" `
  "http://localhost:8080/conversations/$convId/messages" | ConvertFrom-Json | ConvertTo-Json
```

Esperado: array `messages` com 2 itens (role=user e role=assistant).

- [ ] **Step 8: Commit final**

```bash
git add .
git commit -m "feat: plano5 complete — evolution api local + conversations UI"
```

---

## Referência Rápida: URLs Locais

| Serviço | URL |
|---|---|
| Frontend | `http://localhost:3000` |
| API + Swagger | `http://localhost:8080/swagger` |
| Evolution API | `http://localhost:8081` |
| Evolution API (dentro da rede Docker) | `http://evolution-api:8080` |
| Webhook URL template | `http://atendefy-api:8080/webhooks/evolution?token={accountId_sem_tracos}` |

## Referência Rápida: Comandos Docker

```powershell
# Subir tudo (sem evolution)
cd infra && docker compose up -d

# Subir evolution (após override estar configurado)
docker compose up -d evolution-api

# Rebuildar API
docker compose up -d --build atendefy-api

# Rebuildar web
docker compose up -d --build atendefy-web

# Rebuildar tudo
docker compose up -d --build

# Logs da API
docker compose logs atendefy-api -f --tail=50

# Logs do evolution
docker compose logs evolution-api -f --tail=30
```

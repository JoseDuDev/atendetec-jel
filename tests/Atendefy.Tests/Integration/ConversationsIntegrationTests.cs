using Atendefy.API.Modules.Chatbot.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atendefy.Tests.Integration;

[Collection("Integration")]
public class ConversationsIntegrationTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public ConversationsIntegrationTests(ApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var dbFactory = _factory.Services.GetRequiredService<Atendefy.API.Infrastructure.Database.TenantDbContextFactory>();
        await using var db = dbFactory.Create(ApiFactory.TenantSchemaName);
        await db.Database.EnsureCreatedAsync();

        if (!db.Conversations.Any())
        {
            db.Conversations.Add(new Conversation
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                ContactPhone = "5511900000001",
                StartedAt = DateTime.UtcNow.AddHours(-2),
                IsResolved = false
            });
            db.Conversations.Add(new Conversation
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                ContactPhone = "5511900000002",
                StartedAt = DateTime.UtcNow.AddHours(-1),
                IsResolved = true,
                ResolvedAt = DateTime.UtcNow.AddMinutes(-30)
            });
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetConversations_WithoutAuth_Returns401()
    {
        var client = _factory.CreateTenantClient();

        var response = await client.GetAsync("/conversations?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_WithAuth_Returns200WithList()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/conversations?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("conversations").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        body.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetConversations_FilterByOpen_ReturnsOnlyOpen()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/conversations?page=1&pageSize=10&status=open");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("conversations").EnumerateArray().ToList();
        items.Should().OnlyContain(c => !c.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task GetConversations_FilterByResolved_ReturnsOnlyResolved()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/conversations?page=1&pageSize=10&status=resolved");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("conversations").EnumerateArray().ToList();
        items.Should().OnlyContain(c => c.GetProperty("isResolved").GetBoolean());
    }

    [Fact]
    public async Task GetConversationMessages_WhenNotFound_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/conversations/{Guid.NewGuid()}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConversationMessages_WhenFound_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

        var response = await client.GetAsync($"/conversations/{id}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

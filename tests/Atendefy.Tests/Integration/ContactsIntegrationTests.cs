using Atendefy.API.Modules.Chatbot.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Atendefy.Tests.Integration;

[Collection("Integration")]
public class ContactsIntegrationTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private const string SeededPhone = "5511988880001";

    public ContactsIntegrationTests(ApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var dbFactory = _factory.Services.GetRequiredService<Atendefy.API.Infrastructure.Database.TenantDbContextFactory>();
        await using var db = dbFactory.Create(ApiFactory.TenantSchemaName);
        await db.Database.EnsureCreatedAsync();

        if (!db.Contacts.Any(c => c.Phone == SeededPhone))
        {
            db.Contacts.Add(new Contact { Phone = SeededPhone, Name = "Nome Original" });
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetContacts_WithoutAuth_Returns401()
    {
        var client = _factory.CreateTenantClient();

        var response = await client.GetAsync("/contacts?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContacts_WithAuth_Returns200WithList()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/contacts?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        body.GetProperty("contacts").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task PatchContact_WithAuth_UpdatesName()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PatchAsJsonAsync($"/contacts/{SeededPhone}", new { name = "Nome Atualizado" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Nome Atualizado");
        body.GetProperty("phone").GetString().Should().Be(SeededPhone);
    }

    [Fact]
    public async Task PatchContact_WhenNotFound_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PatchAsJsonAsync("/contacts/5500000000000", new { name = "Qualquer" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContacts_Pagination_RespectsPageSizeParam()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/contacts?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("contacts").GetArrayLength().Should().BeLessThanOrEqualTo(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(1);
    }
}

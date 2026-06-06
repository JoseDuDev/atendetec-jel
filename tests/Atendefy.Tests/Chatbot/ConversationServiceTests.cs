using Atendefy.API.Infrastructure.Cache;
using Atendefy.API.Modules.AI.Models;
using Atendefy.API.Modules.Chatbot;
using FluentAssertions;
using NSubstitute;
using StackExchange.Redis;

namespace Atendefy.Tests.Chatbot;

public class ConversationServiceTests
{
    private static (RedisService redis, IDatabase db) CreateRedis()
    {
        var db = Substitute.For<IDatabase>();
        var conn = Substitute.For<IConnectionMultiplexer>();
        conn.GetDatabase().Returns(db);
        return (new RedisService(conn), db);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_WhenNewSession_ShouldReturnEmptyList()
    {
        var (redis, db) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
          .Returns(new RedisValue());   // null/empty redis value

        var sut = new ConversationService(redis);
        var messages = await sut.GetOrCreateSessionAsync("tenant_abc", "5511999999999");

        messages.Should().BeEmpty();
    }

    [Fact]
    public void BuildContextMessages_ShouldAppendUserMessageToHistory()
    {
        var history = new List<ChatMessage>
        {
            new("user", "Qual o horário?"),
            new("assistant", "Das 8h às 18h.")
        };

        var context = ConversationService.BuildContextMessages(history, "Nova pergunta");

        context.Should().HaveCount(3);
        context.Last().Role.Should().Be("user");
        context.Last().Content.Should().Be("Nova pergunta");
    }
}

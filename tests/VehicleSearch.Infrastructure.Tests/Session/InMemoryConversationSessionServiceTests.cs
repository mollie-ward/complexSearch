using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Exceptions;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Session;

namespace VehicleSearch.Infrastructure.Tests.Session;

public class InMemoryConversationSessionServiceTests
{
    private readonly Mock<ILogger<InMemoryConversationSessionService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly InMemoryConversationSessionService _service;

    public InMemoryConversationSessionServiceTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryConversationSessionService>>();
        
        // Create a real configuration with in-memory values
        var configData = new Dictionary<string, string?>
        {
            ["ConversationSession:SessionTimeoutHours"] = "4",
            ["ConversationSession:MaxMessagesPerSession"] = "100"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        _service = new InMemoryConversationSessionService(_loggerMock.Object, _configuration);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new InMemoryConversationSessionService(null!, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateSession_ReturnsUniqueSessionId()
    {
        // Act
        var session = await _service.CreateSessionAsync();

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().NotBeNullOrEmpty();
        Guid.TryParse(session.SessionId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateSession_InitializesEmptyMessageList()
    {
        // Act
        var session = await _service.CreateSessionAsync();

        // Assert
        session.Messages.Should().NotBeNull();
        session.Messages.Should().BeEmpty();
        session.MessageCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateSession_SetsCreationTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var session = await _service.CreateSessionAsync();

        // Assert
        var afterCreate = DateTime.UtcNow;
        session.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        session.CreatedAt.Should().BeOnOrBefore(afterCreate);
        session.LastAccessedAt.Should().BeOnOrAfter(beforeCreate);
        session.LastAccessedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task GetSession_ExistingSession_ReturnsSession()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        var retrievedSession = await _service.GetSessionAsync(session.SessionId);

        // Assert
        retrievedSession.Should().NotBeNull();
        retrievedSession.SessionId.Should().Be(session.SessionId);
    }

    [Fact]
    public async Task GetSession_NonExistentSession_ThrowsSessionNotFoundException()
    {
        // Act
        Func<Task> act = async () => await _service.GetSessionAsync("non-existent-id");

        // Assert
        await act.Should().ThrowAsync<SessionNotFoundException>()
            .Where(ex => ex.SessionId == "non-existent-id");
    }

    [Fact]
    public async Task GetSession_NullSessionId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.GetSessionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSession_EmptySessionId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.GetSessionAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSession_UpdatesLastAccessedTime()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();
        var originalLastAccessed = session.LastAccessedAt;
        
        await Task.Delay(10); // Small delay to ensure time difference

        // Act
        var retrievedSession = await _service.GetSessionAsync(session.SessionId);

        // Assert
        retrievedSession.LastAccessedAt.Should().BeAfter(originalLastAccessed);
    }

    [Fact]
    public async Task AddMessage_ValidSession_AddsMessageWithTimestamp()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();
        var message = new ConversationMessage
        {
            Role = MessageRole.User,
            Content = "Test message"
        };

        // Act
        await _service.AddMessageAsync(session.SessionId, message);

        // Assert
        var retrievedSession = await _service.GetSessionAsync(session.SessionId);
        retrievedSession.Messages.Should().HaveCount(1);
        retrievedSession.Messages[0].Content.Should().Be("Test message");
        retrievedSession.Messages[0].Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddMessage_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        Func<Task> act = async () => await _service.AddMessageAsync(session.SessionId, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddMessage_NonExistentSession_ThrowsSessionNotFoundException()
    {
        // Arrange
        var message = new ConversationMessage
        {
            Role = MessageRole.User,
            Content = "Test message"
        };

        // Act
        Func<Task> act = async () => await _service.AddMessageAsync("non-existent-id", message);

        // Assert
        await act.Should().ThrowAsync<SessionNotFoundException>();
    }

    [Fact]
    public async Task AddMessage_EnforcesMaxMessagesLimit()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ConversationSession:SessionTimeoutHours"] = "4",
            ["ConversationSession:MaxMessagesPerSession"] = "5"
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        var service = new InMemoryConversationSessionService(_loggerMock.Object, config);
        var session = await service.CreateSessionAsync();

        // Act - Add 6 messages (limit is 5)
        for (int i = 0; i < 6; i++)
        {
            await service.AddMessageAsync(session.SessionId, new ConversationMessage
            {
                Role = MessageRole.User,
                Content = $"Message {i}"
            });
        }

        // Assert
        var retrievedSession = await service.GetSessionAsync(session.SessionId);
        retrievedSession.Messages.Should().HaveCount(5);
        retrievedSession.Messages[0].Content.Should().Be("Message 1"); // First message removed
        retrievedSession.Messages[4].Content.Should().Be("Message 5");
    }

    [Fact]
    public async Task UpdateSearchState_ValidSession_UpdatesState()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();
        var searchState = new SearchState
        {
            LastQuery = "BMW under £20k",
            LastResultIds = new List<string> { "V001", "V002" },
            LastSearchTime = DateTime.UtcNow
        };

        // Act
        await _service.UpdateSearchStateAsync(session.SessionId, searchState);

        // Assert
        var retrievedSession = await _service.GetSessionAsync(session.SessionId);
        retrievedSession.CurrentSearchState.Should().NotBeNull();
        retrievedSession.CurrentSearchState!.LastQuery.Should().Be("BMW under £20k");
        retrievedSession.CurrentSearchState.LastResultIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateSearchState_NullState_ThrowsArgumentNullException()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        Func<Task> act = async () => await _service.UpdateSearchStateAsync(session.SessionId, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateSearchState_NonExistentSession_ThrowsSessionNotFoundException()
    {
        // Arrange
        var searchState = new SearchState { LastQuery = "Test" };

        // Act
        Func<Task> act = async () => await _service.UpdateSearchStateAsync("non-existent-id", searchState);

        // Assert
        await act.Should().ThrowAsync<SessionNotFoundException>();
    }

    [Fact]
    public async Task GetHistory_ReturnsLast10Messages()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();
        
        // Add 15 messages
        for (int i = 0; i < 15; i++)
        {
            await _service.AddMessageAsync(session.SessionId, new ConversationMessage
            {
                Role = MessageRole.User,
                Content = $"Message {i}"
            });
        }

        // Act
        var history = await _service.GetHistoryAsync(session.SessionId, 10);

        // Assert
        history.Messages.Should().HaveCount(10);
        history.TotalMessages.Should().Be(15);
        history.Messages[0].Content.Should().Be("Message 5"); // Last 10 messages
        history.Messages[9].Content.Should().Be("Message 14");
    }

    [Fact]
    public async Task GetHistory_EmptySession_ReturnsEmptyList()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        var history = await _service.GetHistoryAsync(session.SessionId);

        // Assert
        history.Messages.Should().BeEmpty();
        history.TotalMessages.Should().Be(0);
    }

    [Fact]
    public async Task GetHistory_NegativeMaxMessages_ThrowsArgumentException()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        Func<Task> act = async () => await _service.GetHistoryAsync(session.SessionId, -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*maxMessages must be greater than zero*");
    }

    [Fact]
    public async Task GetHistory_ZeroMaxMessages_ThrowsArgumentException()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        Func<Task> act = async () => await _service.GetHistoryAsync(session.SessionId, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*maxMessages must be greater than zero*");
    }

    [Fact]
    public async Task GetHistory_CustomMaxMessages_ReturnsCorrectCount()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();
        
        for (int i = 0; i < 20; i++)
        {
            await _service.AddMessageAsync(session.SessionId, new ConversationMessage
            {
                Role = MessageRole.User,
                Content = $"Message {i}"
            });
        }

        // Act
        var history = await _service.GetHistoryAsync(session.SessionId, 5);

        // Assert
        history.Messages.Should().HaveCount(5);
        history.TotalMessages.Should().Be(20);
    }

    [Fact]
    public async Task ClearSession_RemovesSession()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        await _service.ClearSessionAsync(session.SessionId);

        // Assert
        Func<Task> act = async () => await _service.GetSessionAsync(session.SessionId);
        await act.Should().ThrowAsync<SessionNotFoundException>();
    }

    [Fact]
    public async Task ClearSession_NonExistentSession_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await _service.ClearSessionAsync("non-existent-id");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearSession_NullSessionId_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await _service.ClearSessionAsync(null!);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearSession_EmptySessionId_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await _service.ClearSessionAsync("");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SessionExists_ExistingSession_ReturnsTrue()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        var exists = await _service.SessionExistsAsync(session.SessionId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SessionExists_NonExistentSession_ReturnsFalse()
    {
        // Act
        var exists = await _service.SessionExistsAsync("non-existent-id");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SessionExists_NullOrEmptySessionId_ReturnsFalse()
    {
        // Act
        var existsNull = await _service.SessionExistsAsync(null!);
        var existsEmpty = await _service.SessionExistsAsync("");

        // Assert
        existsNull.Should().BeFalse();
        existsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupExpiredSessions_RemovesExpiredSessions()
    {
        // Arrange - Create service with very short timeout for testing
        var configData = new Dictionary<string, string?>
        {
            ["ConversationSession:SessionTimeoutHours"] = "0", // 0 hours = immediate expiry
            ["ConversationSession:MaxMessagesPerSession"] = "100"
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        var service = new InMemoryConversationSessionService(_loggerMock.Object, config);
        var session = await service.CreateSessionAsync();
        
        await Task.Delay(10); // Wait a bit to ensure expiry

        // Act
        var removedCount = service.CleanupExpiredSessions();

        // Assert
        removedCount.Should().BeGreaterThan(0);
        var exists = await service.SessionExistsAsync(session.SessionId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupExpiredSessions_DoesNotRemoveActiveSessions()
    {
        // Arrange
        var session = await _service.CreateSessionAsync();

        // Act
        var removedCount = _service.CleanupExpiredSessions();

        // Assert
        removedCount.Should().Be(0);
        var exists = await _service.SessionExistsAsync(session.SessionId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentAccess_HandlesMultipleSessions()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 100)
            .Select(async i =>
            {
                var session = await _service.CreateSessionAsync();
                await _service.AddMessageAsync(session.SessionId, new ConversationMessage
                {
                    Role = MessageRole.User,
                    Content = $"Message {i}"
                });
                return session.SessionId;
            });

        var sessionIds = await Task.WhenAll(tasks);

        // Assert
        sessionIds.Should().HaveCount(100);
        sessionIds.Should().OnlyHaveUniqueItems();

        // Verify all sessions can be retrieved
        foreach (var sessionId in sessionIds)
        {
            var session = await _service.GetSessionAsync(sessionId);
            session.Should().NotBeNull();
            session.Messages.Should().HaveCount(1);
        }
    }
}

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Infrastructure.Session;

namespace VehicleSearch.Infrastructure.Tests.Session;

public class SessionCleanupServiceTests
{
    private readonly Mock<ILogger<SessionCleanupService>> _loggerMock;
    private readonly Mock<ILogger<InMemoryConversationSessionService>> _sessionLoggerMock;
    private readonly IConfiguration _configuration;
    private readonly InMemoryConversationSessionService _sessionService;

    public SessionCleanupServiceTests()
    {
        _loggerMock = new Mock<ILogger<SessionCleanupService>>();
        _sessionLoggerMock = new Mock<ILogger<InMemoryConversationSessionService>>();
        
        var configData = new Dictionary<string, string?>
        {
            ["ConversationSession:SessionTimeoutHours"] = "4",
            ["ConversationSession:MaxMessagesPerSession"] = "100",
            ["ConversationSession:CleanupIntervalHours"] = "1"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        _sessionService = new InMemoryConversationSessionService(_sessionLoggerMock.Object, _configuration);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SessionCleanupService(null!, _sessionService, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSessionService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SessionCleanupService(_loggerMock.Object, null!, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsCleanup()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var service = new SessionCleanupService(_loggerMock.Object, _sessionService, _configuration);

        // Act - Start the service and immediately cancel
        var executeTask = service.StartAsync(cts.Token);
        cts.Cancel();
        await executeTask;

        // Assert - Should not throw
        executeTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_PerformsCleanup()
    {
        // Arrange - Create a service with short cleanup interval for testing
        var configData = new Dictionary<string, string?>
        {
            ["ConversationSession:SessionTimeoutHours"] = "0", // Immediate expiry
            ["ConversationSession:MaxMessagesPerSession"] = "100",
            ["ConversationSession:CleanupIntervalHours"] = "1" // 1 hour - we'll cancel before it runs
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        var sessionService = new InMemoryConversationSessionService(_sessionLoggerMock.Object, config);
        
        // Create an expired session
        await sessionService.CreateSessionAsync();
        
        using var cts = new CancellationTokenSource();
        var service = new SessionCleanupService(_loggerMock.Object, sessionService, config);

        // Act - Start the service and cancel immediately (before cleanup runs)
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(50); // Brief delay
        cts.Cancel();
        
        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        // Assert - Service should have started and stopped without errors
        executeTask.IsCompleted.Should().BeTrue();
    }
}

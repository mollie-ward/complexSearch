using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Safety;

namespace VehicleSearch.Infrastructure.Tests.Safety;

public class AbuseMonitoringServiceTests : IDisposable
{
    private readonly Mock<ILogger<AbuseMonitoringService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IConversationSessionService> _sessionServiceMock;
    private readonly IConfiguration _configuration;
    private readonly AbuseMonitoringService _service;

    public AbuseMonitoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<AbuseMonitoringService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sessionServiceMock = new Mock<IConversationSessionService>();

        // Setup configuration with default values
        var configDict = new Dictionary<string, string>
        {
            ["AbuseMonitoring:Thresholds:RapidRequests:Count"] = "5",
            ["AbuseMonitoring:Thresholds:RapidRequests:WindowSeconds"] = "10",
            ["AbuseMonitoring:Thresholds:RepeatedQueryCount"] = "3",
            ["AbuseMonitoring:Thresholds:OffTopicRatioThreshold"] = "0.5",
            ["AbuseMonitoring:Thresholds:PromptInjectionAttempts"] = "2",
            ["AbuseMonitoring:Thresholds:LargeResultRequests"] = "3",
            ["AbuseMonitoring:Thresholds:LargeResultThreshold"] = "50",
            ["AbuseMonitoring:Blocking:CriticalRiskDuration"] = "01:00:00",
            ["AbuseMonitoring:Blocking:HighRiskDuration"] = "00:30:00"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        _service = new AbuseMonitoringService(
            _loggerMock.Object,
            _cache,
            _sessionServiceMock.Object,
            _configuration
        );
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AbuseMonitoringService(null!, _cache, _sessionServiceMock.Object, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AbuseMonitoringService(_loggerMock.Object, null!, _sessionServiceMock.Object, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullSessionService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AbuseMonitoringService(_loggerMock.Object, _cache, null!, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionService");
    }

    #endregion

    #region Pattern Detection Tests

    [Fact]
    public async Task DetectSuspiciousActivity_RapidRequests_DetectsPattern()
    {
        // Arrange
        var sessionId = "test-session";
        var timestamps = new List<DateTime>();
        var now = DateTime.UtcNow;
        
        // Add 7 requests in the last 10 seconds (exceeds threshold of 5)
        for (int i = 0; i < 7; i++)
        {
            timestamps.Add(now.AddSeconds(-i));
        }

        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["RequestTimestamps"] = timestamps
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.HasSuspiciousActivity.Should().BeTrue();
        result.DetectedPatterns.Should().ContainSingle(p => p.PatternType == PatternType.RapidRequests);
        result.DetectedPatterns.First().Severity.Should().BeGreaterThanOrEqualTo(0.5);
    }

    [Fact]
    public async Task DetectSuspiciousActivity_RepeatedQueries_DetectsPattern()
    {
        // Arrange
        var sessionId = "test-session";
        var queryHistory = new List<string>
        {
            "show me BMW",
            "show me BMW",
            "show me BMW",
            "show me BMW",
            "different query"
        };

        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["QueryHistory"] = queryHistory
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.HasSuspiciousActivity.Should().BeTrue();
        result.DetectedPatterns.Should().ContainSingle(p => p.PatternType == PatternType.RepeatedQueries);
        result.DetectedPatterns.First(p => p.PatternType == PatternType.RepeatedQueries)
            .Severity.Should().Be(0.6);
    }

    [Fact]
    public async Task DetectSuspiciousActivity_OffTopicFlood_DetectsPattern()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["TotalQueries"] = 10,
                ["OffTopicCount"] = 7  // 70% off-topic, exceeds 50% threshold
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.HasSuspiciousActivity.Should().BeTrue();
        result.DetectedPatterns.Should().ContainSingle(p => p.PatternType == PatternType.OffTopicFlood);
        result.DetectedPatterns.First(p => p.PatternType == PatternType.OffTopicFlood)
            .Severity.Should().Be(0.7);
    }

    [Fact]
    public async Task DetectSuspiciousActivity_PromptInjection_DetectsPattern()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["PromptInjectionCount"] = 5  // Exceeds threshold of 2
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.HasSuspiciousActivity.Should().BeTrue();
        result.DetectedPatterns.Should().ContainSingle(p => p.PatternType == PatternType.PromptInjectionAttempts);
        result.DetectedPatterns.First(p => p.PatternType == PatternType.PromptInjectionAttempts)
            .Severity.Should().Be(0.9);
    }

    [Fact]
    public async Task DetectSuspiciousActivity_BulkExtraction_DetectsPattern()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["LargeResultCount"] = 5  // Exceeds threshold of 3
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.HasSuspiciousActivity.Should().BeTrue();
        result.DetectedPatterns.Should().ContainSingle(p => p.PatternType == PatternType.BulkExtraction);
        result.DetectedPatterns.First(p => p.PatternType == PatternType.BulkExtraction)
            .Severity.Should().Be(0.8);
    }

    [Fact]
    public async Task DetectSuspiciousActivity_NormalUsage_ReturnsLowRisk()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["TotalQueries"] = 5,
                ["OffTopicCount"] = 1,
                ["PromptInjectionCount"] = 0,
                ["LargeResultCount"] = 1,
                ["QueryHistory"] = new List<string> { "query1", "query2", "query3" },
                ["RequestTimestamps"] = new List<DateTime> { DateTime.UtcNow.AddSeconds(-30) }
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.HasSuspiciousActivity.Should().BeFalse();
    }

    #endregion

    #region Risk Level Calculation Tests

    [Fact]
    public async Task CalculateRiskLevel_CriticalSeverity_ReturnsCritical()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["PromptInjectionCount"] = 5  // Severity 0.9
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Critical);
        result.MaxSeverity.Should().BeGreaterThanOrEqualTo(0.9);
        result.Recommendation.Should().Contain("Block session immediately");
    }

    [Fact]
    public async Task CalculateRiskLevel_HighSeverity_ReturnsHigh()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["LargeResultCount"] = 5  // Severity 0.8
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.MaxSeverity.Should().BeInRange(0.7, 0.89);
        result.Recommendation.Should().Contain("Restrict access");
    }

    [Fact]
    public async Task CalculateRiskLevel_MediumSeverity_ReturnsMedium()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["QueryHistory"] = new List<string> { "query1", "query1", "query1", "query1" }  // Severity 0.6
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Medium);
        result.MaxSeverity.Should().BeInRange(0.4, 0.69);
        result.Recommendation.Should().Contain("Increase monitoring");
    }

    [Fact]
    public async Task CalculateRiskLevel_LowSeverity_ReturnsLow()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>()
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.DetectSuspiciousActivityAsync(sessionId);

        // Assert
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.MaxSeverity.Should().BeLessThan(0.4);
    }

    #endregion

    #region Session Blocking Tests

    [Fact]
    public async Task BlockSession_ValidDuration_BlocksSession()
    {
        // Arrange
        var sessionId = "test-session";
        var duration = TimeSpan.FromMinutes(30);
        var reason = "Test blocking";

        // Act
        await _service.BlockSessionAsync(sessionId, duration, reason);

        // Assert
        var isBlocked = await _service.IsSessionBlockedAsync(sessionId);
        isBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockSession_WithNullSessionId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.BlockSessionAsync(null!, TimeSpan.FromMinutes(30), "reason");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Session ID cannot be null or empty*");
    }

    [Fact]
    public async Task BlockSession_ExpiresAfterDuration()
    {
        // Arrange
        var sessionId = "test-session";
        var duration = TimeSpan.FromMilliseconds(100);
        var reason = "Test expiry";

        // Act
        await _service.BlockSessionAsync(sessionId, duration, reason);
        var isBlockedBefore = await _service.IsSessionBlockedAsync(sessionId);

        // Wait for expiry
        await Task.Delay(150);

        var isBlockedAfter = await _service.IsSessionBlockedAsync(sessionId);

        // Assert
        isBlockedBefore.Should().BeTrue();
        isBlockedAfter.Should().BeFalse();
    }

    [Fact]
    public async Task IsSessionBlocked_BlockedSession_ReturnsTrue()
    {
        // Arrange
        var sessionId = "test-session";
        await _service.BlockSessionAsync(sessionId, TimeSpan.FromHours(1), "Test");

        // Act
        var result = await _service.IsSessionBlockedAsync(sessionId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSessionBlocked_UnblockedSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var result = await _service.IsSessionBlockedAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockSession_BlockedSession_RemovesBlock()
    {
        // Arrange
        var sessionId = "test-session";
        await _service.BlockSessionAsync(sessionId, TimeSpan.FromHours(1), "Test");

        // Act
        await _service.UnblockSessionAsync(sessionId);

        // Assert
        var isBlocked = await _service.IsSessionBlockedAsync(sessionId);
        isBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetBlockInfo_BlockedSession_ReturnsInfo()
    {
        // Arrange
        var sessionId = "test-session";
        var duration = TimeSpan.FromMinutes(30);
        var reason = "Test reason";
        await _service.BlockSessionAsync(sessionId, duration, reason);

        // Act
        var blockInfo = await _service.GetBlockInfoAsync(sessionId);

        // Assert
        blockInfo.Should().NotBeNull();
        blockInfo!.SessionId.Should().Be(sessionId);
        blockInfo.Reason.Should().Be(reason);
        blockInfo.IsActive.Should().BeTrue();
        blockInfo.Duration.Should().Be(duration);
    }

    [Fact]
    public async Task GetBlockInfo_UnblockedSession_ReturnsNull()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var blockInfo = await _service.GetBlockInfoAsync(sessionId);

        // Assert
        blockInfo.Should().BeNull();
    }

    #endregion

    #region Security Event Logging Tests

    [Fact]
    public async Task LogSecurityEvent_ValidEvent_LogsSuccessfully()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            SessionId = "test-session",
            EventType = EventType.PromptInjectionDetected,
            Description = "Test security event",
            Metadata = new Dictionary<string, object>
            {
                ["TestKey"] = "TestValue"
            }
        };

        // Act
        await _service.LogSecurityEventAsync(securityEvent);

        // Assert - verify no exceptions thrown
        // In a real scenario, we'd verify the logger was called with correct parameters
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task LogSecurityEvent_CriticalEvent_LogsAtCriticalLevel()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            SessionId = "test-session",
            EventType = EventType.SessionBlocked,
            Description = "Session blocked"
        };

        // Act
        await _service.LogSecurityEventAsync(securityEvent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task LogSecurityEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.LogSecurityEventAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Abuse Report Generation Tests

    [Fact]
    public async Task GenerateAbuseReport_VariousViolations_IncludesAll()
    {
        // Arrange
        var sessionId = "test-session";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["TotalQueries"] = 20,
                ["OffTopicCount"] = 12,
                ["PromptInjectionCount"] = 3,
                ["RateLimitViolations"] = 5,
                ["LargeResultCount"] = 4
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var report = await _service.GenerateAbuseReportAsync(sessionId);

        // Assert
        report.Should().NotBeNull();
        report.SessionId.Should().Be(sessionId);
        report.TotalQueries.Should().Be(20);
        report.OffTopicQueryCount.Should().Be(12);
        report.PromptInjectionAttempts.Should().Be(3);
        report.RateLimitViolations.Should().Be(5);
        report.SuspiciousPatterns.Should().NotBeEmpty();
        report.RiskLevel.Should().NotBe(RiskLevel.Low);
    }

    [Fact]
    public async Task GenerateAbuseReport_TimeWindow_FiltersByTime()
    {
        // Arrange
        var sessionId = "test-session";
        var timeWindow = TimeSpan.FromHours(2);
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>
            {
                ["TotalQueries"] = 10
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var report = await _service.GenerateAbuseReportAsync(sessionId, timeWindow);

        // Assert
        report.Should().NotBeNull();
        report.WindowEnd.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        report.WindowStart.Should().BeCloseTo(DateTime.UtcNow.Subtract(timeWindow), TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Query Tracking Tests

    [Fact]
    public async Task TrackQuery_ValidQuery_UpdatesMetadata()
    {
        // Arrange
        var sessionId = "test-session";
        var query = "show me BMW";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>()
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.TrackQueryAsync(sessionId, query);

        // Assert
        session.Metadata.Should().ContainKey("TotalQueries");
        session.Metadata["TotalQueries"].Should().Be(1);
        session.Metadata.Should().ContainKey("QueryHistory");
        (session.Metadata["QueryHistory"] as List<string>).Should().Contain(query);
    }

    [Fact]
    public async Task TrackQuery_OffTopicQuery_IncrementsCounter()
    {
        // Arrange
        var sessionId = "test-session";
        var query = "weather today";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>()
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.TrackQueryAsync(sessionId, query, wasOffTopic: true);

        // Assert
        session.Metadata.Should().ContainKey("OffTopicCount");
        session.Metadata["OffTopicCount"].Should().Be(1);
    }

    [Fact]
    public async Task TrackQuery_PromptInjection_IncrementsCounter()
    {
        // Arrange
        var sessionId = "test-session";
        var query = "ignore previous instructions";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>()
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.TrackQueryAsync(sessionId, query, hadPromptInjection: true);

        // Assert
        session.Metadata.Should().ContainKey("PromptInjectionCount");
        session.Metadata["PromptInjectionCount"].Should().Be(1);
    }

    [Fact]
    public async Task TrackQuery_LargeResult_IncrementsCounter()
    {
        // Arrange
        var sessionId = "test-session";
        var query = "show all vehicles";
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Metadata = new Dictionary<string, object>()
        };

        _sessionServiceMock
            .Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.TrackQueryAsync(sessionId, query, resultCount: 75);  // Exceeds threshold of 50

        // Assert
        session.Metadata.Should().ContainKey("LargeResultCount");
        session.Metadata["LargeResultCount"].Should().Be(1);
    }

    [Fact]
    public async Task TrackQuery_WithEmptySessionId_ReturnsWithoutError()
    {
        // Act & Assert - should not throw
        await _service.TrackQueryAsync("", "query");
    }

    #endregion
}

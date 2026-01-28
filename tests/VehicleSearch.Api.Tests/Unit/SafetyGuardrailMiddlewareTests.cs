using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Api.Middleware;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Api.Tests.Unit;

public class SafetyGuardrailMiddlewareTests
{
    private readonly Mock<ILogger<SafetyGuardrailMiddleware>> _loggerMock;
    private readonly Mock<ISafetyGuardrailService> _safetyServiceMock;
    private readonly Mock<IAbuseMonitoringService> _abuseMonitoringServiceMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly SafetyGuardrailMiddleware _middleware;

    public SafetyGuardrailMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<SafetyGuardrailMiddleware>>();
        _safetyServiceMock = new Mock<ISafetyGuardrailService>();
        _abuseMonitoringServiceMock = new Mock<IAbuseMonitoringService>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new SafetyGuardrailMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SafetyGuardrailMiddleware(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SafetyGuardrailMiddleware(_nextMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WithNonSearchEndpoint_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/health";

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithSearchEndpointAndValidQuery_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "query", "BMW car" }
        });

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult { IsValid = true });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync("BMW car", "anonymous", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSearchEndpointAndInvalidQuery_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "query", "'; DROP TABLE" }
        });
        context.Response.Body = new MemoryStream();

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.InvalidCharacters,
                Message = "Query contains malicious patterns"
            });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        _nextMock.Verify(n => n(context), Times.Never);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("InvalidCharacters");
        responseBody.Should().Contain("Query contains malicious patterns");
    }

    [Fact]
    public async Task InvokeAsync_WithSessionIdHeader_PassesSessionIdToService()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Headers["X-Session-Id"] = "test-session-123";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "query", "BMW car" }
        });

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult { IsValid = true });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync("BMW car", "test-session-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithQueryInQueryString_ExtractsQuery()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "query", "Find a red car" }
        });

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult { IsValid = true });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync("Find a red car", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithQueryInRequestBody_ExtractsQuery()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        
        var requestBody = JsonSerializer.Serialize(new { query = "Find a blue SUV" });
        var bytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Response.Body = new MemoryStream();

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult { IsValid = true });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync("Find a blue SUV", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitViolation_ReturnsBadRequestWithRetryInfo()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "query", "BMW car" }
        });
        context.Response.Body = new MemoryStream();

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.RateLimitExceeded,
                Message = "Rate limit exceeded. Please try again in 60 seconds."
            });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("RateLimitExceeded");
        responseBody.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task InvokeAsync_WithOffTopicQuery_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/query";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "q", "What's the weather?" }
        });
        context.Response.Body = new MemoryStream();

        _safetyServiceMock.Setup(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.OffTopic,
                Message = "Query is not related to vehicle search"
            });

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("OffTopic");
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyQuery_SkipsValidation()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/search";
        // No query parameter

        // Act
        await _middleware.InvokeAsync(context, _safetyServiceMock.Object, _abuseMonitoringServiceMock.Object);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
        _safetyServiceMock.Verify(s => s.ValidateQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

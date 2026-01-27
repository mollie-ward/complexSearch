using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class IntentClassifierTests
{
    private readonly Mock<IOptions<AzureOpenAIConfig>> _configMock;
    private readonly Mock<ILogger<IntentClassifier>> _loggerMock;
    private readonly AzureOpenAIConfig _config;

    public IntentClassifierTests()
    {
        _config = new AzureOpenAIConfig
        {
            Endpoint = "", // Empty to force pattern matching fallback
            ApiKey = "",
            ChatDeploymentName = "gpt-4"
        };

        _configMock = new Mock<IOptions<AzureOpenAIConfig>>();
        _configMock.Setup(x => x.Value).Returns(_config);
        
        _loggerMock = new Mock<ILogger<IntentClassifier>>();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new IntentClassifier(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new IntentClassifier(_configMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_CreatesInstanceWithPatternMatching()
    {
        // Act
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Assert
        classifier.Should().NotBeNull();
    }

    [Fact]
    public async Task ClassifyAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await classifier.ClassifyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ClassifyAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await classifier.ClassifyAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Theory]
    [InlineData("Show me BMW cars", QueryIntent.Search)]
    [InlineData("Find Audi vehicles", QueryIntent.Search)]
    [InlineData("I want a Mercedes", QueryIntent.Search)]
    [InlineData("Looking for Toyota", QueryIntent.Search)]
    [InlineData("Need a Ford", QueryIntent.Search)]
    public async Task ClassifyAsync_SearchQueries_ReturnsSearchIntent(string query, QueryIntent expectedIntent)
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData("cheaper car", QueryIntent.Refine)]
    [InlineData("bigger car instead", QueryIntent.Refine)]
    [InlineData("more expensive vehicle", QueryIntent.Refine)]
    public async Task ClassifyAsync_RefineQueries_ReturnsRefineIntent(string query, QueryIntent expectedIntent)
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData("Compare BMW to Audi", QueryIntent.Compare)]
    [InlineData("BMW vs Audi", QueryIntent.Compare)]
    [InlineData("Difference between BMW and Audi", QueryIntent.Compare)]
    public async Task ClassifyAsync_CompareQueries_ReturnsCompareIntent(string query, QueryIntent expectedIntent)
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData("How many BMW cars do you have", QueryIntent.Information)]
    [InlineData("Tell me about electric vehicles", QueryIntent.Information)]
    [InlineData("What is the price range", QueryIntent.Information)]
    public async Task ClassifyAsync_InformationQueries_ReturnsInformationIntent(string query, QueryIntent expectedIntent)
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData("What's the weather?", QueryIntent.OffTopic)]
    [InlineData("Hello", QueryIntent.OffTopic)]
    [InlineData("How are you?", QueryIntent.OffTopic)]
    public async Task ClassifyAsync_OffTopicQueries_ReturnsOffTopicIntent(string query, QueryIntent expectedIntent)
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task ClassifyAsync_SameQueryTwice_ReturnsCachedResult()
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);
        var query = "Show me BMW cars";

        // Act
        var result1 = await classifier.ClassifyAsync(query);
        var result2 = await classifier.ClassifyAsync(query);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task ClassifyAsync_WithContext_PassesContextAndClassifiesAsRefine()
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);
        var query = "cheaper car";
        var context = new ConversationContext
        {
            SessionId = "test",
            History = new List<string> { "Show me BMW cars" }
        };

        // Act
        var (intent, confidence) = await classifier.ClassifyAsync(query, context);

        // Assert
        // With refine keyword "cheaper" and vehicle keyword "car", should be Refine
        intent.Should().Be(QueryIntent.Refine);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task ClassifyAsync_VehicleRelatedQuery_DoesNotReturnOffTopic()
    {
        // Arrange
        var classifier = new IntentClassifier(_configMock.Object, _loggerMock.Object);
        var query = "Show me BMW cars under £20,000";

        // Act
        var (intent, _) = await classifier.ClassifyAsync(query);

        // Assert
        intent.Should().NotBe(QueryIntent.OffTopic);
    }
}

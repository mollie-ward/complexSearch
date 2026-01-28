using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests.AI;

public class ReferenceResolverServiceTests
{
    private readonly Mock<ILogger<ReferenceResolverService>> _loggerMock;
    private readonly Mock<ComparativeResolver> _comparativeResolverMock;
    private readonly Mock<QueryRefiner> _queryRefinerMock;
    private readonly ReferenceResolverService _service;

    public ReferenceResolverServiceTests()
    {
        _loggerMock = new Mock<ILogger<ReferenceResolverService>>();
        var comparativeLoggerMock = new Mock<ILogger<ComparativeResolver>>();
        _comparativeResolverMock = new Mock<ComparativeResolver>(comparativeLoggerMock.Object);
        var queryRefinerLoggerMock = new Mock<ILogger<QueryRefiner>>();
        var attributeMapperMock = new Mock<VehicleSearch.Core.Interfaces.IAttributeMapperService>();
        var queryComposerMock = new Mock<VehicleSearch.Core.Interfaces.IQueryComposerService>();
        _queryRefinerMock = new Mock<QueryRefiner>(
            queryRefinerLoggerMock.Object,
            attributeMapperMock.Object,
            queryComposerMock.Object);

        _service = new ReferenceResolverService(
            _loggerMock.Object,
            _comparativeResolverMock.Object,
            _queryRefinerMock.Object);
    }

    #region ExtractReferentsAsync Tests

    [Fact]
    public async Task ExtractReferentsAsync_WithSingularPronoun_ReturnsReference()
    {
        // Arrange
        var query = "Tell me more about it";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.ReferenceText == "it" && r.Type == ReferenceType.Pronoun);
    }

    [Fact]
    public async Task ExtractReferentsAsync_WithPluralPronoun_ReturnsReference()
    {
        // Arrange
        var query = "Show me cheaper ones from them";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.ReferenceText == "them" && r.Type == ReferenceType.Pronoun);
    }

    [Fact]
    public async Task ExtractReferentsAsync_WithPositionalReference_ReturnsReference()
    {
        // Arrange
        var query = "Show me the first one";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(r => r.Type == ReferenceType.Anaphoric && r.ReferenceText.Contains("first"));
    }

    [Fact]
    public async Task ExtractReferentsAsync_WithComparative_ReturnsReference()
    {
        // Arrange
        var query = "Show me cheaper options";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.ReferenceText == "cheaper" && r.Type == ReferenceType.Comparative);
    }

    [Fact]
    public async Task ExtractReferentsAsync_WithMultipleReferences_ReturnsAllReferences()
    {
        // Arrange
        var query = "Show me the first one, but cheaper and newer";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(1);
        result.Should().Contain(r => r.Type == ReferenceType.Anaphoric);
        result.Should().Contain(r => r.ReferenceText == "cheaper");
        result.Should().Contain(r => r.ReferenceText == "newer");
    }

    [Fact]
    public async Task ExtractReferentsAsync_WithNoReferences_ReturnsEmptyList()
    {
        // Arrange
        var query = "Show me BMW cars";

        // Act
        var result = await _service.ExtractReferentsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region ResolveReferencesAsync Tests - Singular Pronouns

    [Fact]
    public async Task ResolveReferencesAsync_ItPronoun_ResolvesToLastSingleVehicle()
    {
        // Arrange
        var query = "Tell me more about it";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.OriginalQuery.Should().Be(query);
        result.ResolvedValues.Should().ContainKey("vehicle_id");
        result.ResolvedValues["vehicle_id"].Should().Be("V001");
        result.HasUnresolvedReferences.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReferencesAsync_ItPronoun_NoSingleResult_ReturnsUnresolved()
    {
        // Arrange
        var query = "Tell me more about it";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002", "V003" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.HasUnresolvedReferences.Should().BeTrue();
        result.UnresolvedMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResolveReferencesAsync_ItPronoun_NoPreviousResults_ReturnsUnresolved()
    {
        // Arrange
        var query = "Tell me more about it";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState()
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.HasUnresolvedReferences.Should().BeTrue();
        result.UnresolvedMessage.Should().Contain("don't have a specific vehicle");
    }

    #endregion

    #region ResolveReferencesAsync Tests - Plural Pronouns

    [Fact]
    public async Task ResolveReferencesAsync_ThemPronoun_ResolvesToLastResultSet()
    {
        // Arrange
        var query = "Show me cheaper ones from them";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002", "V003" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.ResolvedValues.Should().ContainKey("vehicle_ids");
        var vehicleIds = result.ResolvedValues["vehicle_ids"] as List<string>;
        vehicleIds.Should().HaveCount(3);
        vehicleIds.Should().Contain("V001");
        vehicleIds.Should().Contain("V002");
        vehicleIds.Should().Contain("V003");
    }

    [Fact]
    public async Task ResolveReferencesAsync_ThemPronoun_NoResults_ReturnsUnresolved()
    {
        // Arrange
        var query = "Show me cheaper ones from them";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState()
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.HasUnresolvedReferences.Should().BeTrue();
        result.UnresolvedMessage.Should().Contain("don't have previous search results");
    }

    #endregion

    #region ResolveReferencesAsync Tests - Positional References

    [Fact]
    public async Task ResolveReferencesAsync_FirstOne_ResolvesToFirstResult()
    {
        // Arrange
        var query = "Show me the first one";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002", "V003" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.ResolvedValues.Should().ContainKey("vehicle_id");
        result.ResolvedValues["vehicle_id"].Should().Be("V001");
        result.HasUnresolvedReferences.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReferencesAsync_SecondOne_ResolvesToSecondResult()
    {
        // Arrange
        var query = "Show me the second car";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002", "V003" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.ResolvedValues.Should().ContainKey("vehicle_id");
        result.ResolvedValues["vehicle_id"].Should().Be("V002");
        result.HasUnresolvedReferences.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReferencesAsync_LastOne_ResolvesToLastResult()
    {
        // Arrange
        var query = "Show me the last vehicle";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002", "V003", "V004", "V005" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.ResolvedValues.Should().ContainKey("vehicle_id");
        result.ResolvedValues["vehicle_id"].Should().Be("V005");
        result.HasUnresolvedReferences.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReferencesAsync_PositionalReference_IndexOutOfBounds_ReturnsUnresolved()
    {
        // Arrange
        var query = "Show me the fifth one";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState
            {
                LastResultIds = new List<string> { "V001", "V002" }
            }
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.HasUnresolvedReferences.Should().BeTrue();
        result.UnresolvedMessage.Should().Contain("only have 2 result");
    }

    #endregion

    #region ResolveReferencesAsync Tests - Edge Cases

    [Fact]
    public async Task ResolveReferencesAsync_NoReferences_ReturnsOriginalQuery()
    {
        // Arrange
        var query = "Show me BMW cars";
        var session = new ConversationSession
        {
            SessionId = "test-session",
            CurrentSearchState = new SearchState()
        };

        // Act
        var result = await _service.ResolveReferencesAsync(query, session);

        // Assert
        result.Should().NotBeNull();
        result.OriginalQuery.Should().Be(query);
        result.ResolvedQueryText.Should().Be(query);
        result.ResolvedReferences.Should().BeEmpty();
        result.HasUnresolvedReferences.Should().BeFalse();
    }

    #endregion
}

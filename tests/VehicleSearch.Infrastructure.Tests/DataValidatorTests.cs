using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Infrastructure.Data;

namespace VehicleSearch.Infrastructure.Tests;

public class DataValidatorTests
{
    private readonly Mock<ILogger<DataValidator>> _loggerMock;
    private readonly DataValidator _validator;

    public DataValidatorTests()
    {
        _loggerMock = new Mock<ILogger<DataValidator>>();
        _validator = new DataValidator(_loggerMock.Object);
    }

    [Fact]
    public void Validate_MissingRegistrationNumber_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "", Make = "Ford", Model = "Focus", Price = 15000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Registration Number");
    }

    [Fact]
    public void Validate_MissingMake_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "", Model = "Focus", Price = 15000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Make");
    }

    [Fact]
    public void Validate_MissingModel_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "", Price = 15000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Model");
    }

    [Fact]
    public void Validate_InvalidPrice_ZeroOrNegative_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 0 },
            new() { Id = "TEST2", Make = "Ford", Model = "Fiesta", Price = -1000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Errors.Should().Contain(e => e.FieldName == "Price");
    }

    [Fact]
    public void Validate_InvalidPrice_TooHigh_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 250000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Price" && e.Message.Contains("less than 200,000"));
    }

    [Fact]
    public void Validate_InvalidMileage_Negative_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 15000, Mileage = -1000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Mileage");
    }

    [Fact]
    public void Validate_InvalidMileage_TooHigh_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 15000, Mileage = 600000 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Mileage" && e.Message.Contains("less than 500,000"));
    }

    [Fact]
    public void Validate_InvalidEngineSize_TooLarge_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 15000, EngineSize = 12.0m }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Engine Size");
    }

    [Fact]
    public void Validate_InvalidRegistrationDate_TooOld_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 15000, RegistrationDate = new DateTime(1985, 1, 1) }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Registration Date");
    }

    [Fact]
    public void Validate_InvalidNumberOfDoors_ReturnsError()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "Ford", Model = "Focus", Price = 15000, NumberOfDoors = 6 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.FieldName == "Number Of Doors");
    }

    [Fact]
    public void Validate_ValidData_ReturnsSuccess()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() 
            { 
                Id = "TEST1", 
                Make = "Ford", 
                Model = "Focus", 
                Price = 15000, 
                Mileage = 25000,
                EngineSize = 1.5m,
                NumberOfDoors = 5,
                RegistrationDate = new DateTime(2020, 5, 15)
            }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MultipleErrors_CapturesAll()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "", Make = "", Model = "", Price = 0, Mileage = -100 }
        };

        // Act
        var result = _validator.Validate(vehicles);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(5); // Id, Make, Model, Price, Mileage
    }
}

using DragonPlacementDataLayer.Models;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Repositories;
using Moq;
using Shouldly;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DragonPlacementTests;

public class DragonTests
{
    [Fact]
    public async Task CreateDragon_ValidInput_ExpectInsertionOfRecordAndSavesOnce()
    {
        var dragon = new Dragon
        {
            GivenName = "Fluffy",
            CanBreathFire = 1,
            CanTakePassengers = 0,
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b"
        };
        var insertedDragon = new Immutable<Dragon>();
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Insert(It.IsAny<Dragon>()))
            .Callback<Dragon>(insertedDragon.Set);

        //Act
        await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, dragon);

        //Assert
        insertedDragon.Get().ShouldBeEquivalentTo(dragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null, "GivenName",         "is required")]
    [InlineData("",   "GivenName",         "is required")]
    [InlineData(2,    "CanBreathFire",     "must be 0 or 1")]
    [InlineData(-1,   "CanBreathFire",     "must be 0 or 1")]
    [InlineData(2,    "CanTakePassengers", "must be 0 or 1")]
    [InlineData(-1,   "CanTakePassengers", "must be 0 or 1")]
    [InlineData(-1,   "WeightInKg",        "must be a positive number")]
    [InlineData(-1,   "LengthInMeters",    "must be a positive number")]
    [InlineData("x",  "FightingSkills",    "must be 'b', 'm', or 'a'")]
    public async Task CreateDragon_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var dragon = new Dragon
        {
            GivenName = "Fluffy",
            CanBreathFire = 1,
            CanTakePassengers = 0,
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b"
        };
        typeof(Dragon).GetProperty(expectedFailureField)!.SetValue(dragon, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        var actualMessage = typeof(DragonValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task CreateDragon_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var dragon = new Dragon
        {
            GivenName = null!,
            CanBreathFire = 2,
            CanTakePassengers = -1,
            WeightInKg = -1,
            LengthInMeters = -1,
            FightingSkills = "x"
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new DragonValidationFailures
        {
            GivenName = "is required",
            CanBreathFire = "must be 0 or 1",
            CanTakePassengers = "must be 0 or 1",
            WeightInKg = "must be a positive number",
            LengthInMeters = "must be a positive number",
            FightingSkills = "must be 'b', 'm', or 'a'"
        });
    }

    [Fact]
    public async Task UpdateDragon_ValidInput_ExpectUpdateOfRecordAndSavesOnce()
    {
        var dragonId = 1;
        var existingDragon = new Dragon
        {
            DragonId = dragonId,
            GivenName = "Old Name",
            FamilyName = "Old Family",
            CanBreathFire = 0,
            CanTakePassengers = 0,
            WeightInKg = 5,
            LengthInMeters = 3,
            FightingSkills = "b"
        };
        var updatedDragon = new Dragon
        {
            DragonId = dragonId,
            GivenName = "New Name",
            FamilyName = "New Family",
            CanBreathFire = 1,
            CanTakePassengers = 1,
            WeightInKg = 20,
            LengthInMeters = 10,
            FightingSkills = "a"
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.GetByID(dragonId)).ReturnsAsync(existingDragon);

        //Act
        await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, dragonId, updatedDragon);

        //Assert
        existingDragon.ShouldBeEquivalentTo(updatedDragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDragon_DragonNotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.GetByID(It.IsAny<object>())).ReturnsAsync((Dragon?)null);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, 999, new Dragon());

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData(" ",  "GivenName",         "is required")]
    [InlineData(5,    "CanBreathFire",     "must be 0 or 1")]
    [InlineData(-3,   "CanBreathFire",     "must be 0 or 1")]
    [InlineData(3,    "CanTakePassengers", "must be 0 or 1")]
    [InlineData(-2,   "CanTakePassengers", "must be 0 or 1")]
    [InlineData(-5,   "WeightInKg",        "must be a positive number")]
    [InlineData(-10,  "LengthInMeters",    "must be a positive number")]
    [InlineData("c",  "FightingSkills",    "must be 'b', 'm', or 'a'")]
    public async Task UpdateDragon_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var dragon = new Dragon
        {
            GivenName = "Thunderclaw",
            CanBreathFire = 1,
            CanTakePassengers = 0,
            WeightInKg = 50,
            LengthInMeters = 8,
            FightingSkills = "m"
        };
        typeof(Dragon).GetProperty(expectedFailureField)!.SetValue(dragon, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, 1, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        var actualMessage = typeof(DragonValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task UpdateDragon_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var dragon = new Dragon
        {
            GivenName = " ",
            CanBreathFire = 5,
            CanTakePassengers = -2,
            WeightInKg = -5,
            LengthInMeters = -10,
            FightingSkills = "c"
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, 1, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new DragonValidationFailures
        {
            GivenName = "is required",
            CanBreathFire = "must be 0 or 1",
            CanTakePassengers = "must be 0 or 1",
            WeightInKg = "must be a positive number",
            LengthInMeters = "must be a positive number",
            FightingSkills = "must be 'b', 'm', or 'a'"
        });
    }
}

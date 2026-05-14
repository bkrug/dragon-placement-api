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
        insertedDragon.ShouldBeEquivalentTo(dragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }
}

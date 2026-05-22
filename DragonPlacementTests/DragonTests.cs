using System.Linq.Expressions;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonPlacementTests;

public class DragonTests
{
    [Fact]
    public async Task CreateDragon_ValidInput_ExpectInsertionOfRecordAndSavesOnce()
    {
        List<SkillTag> skills = [
            new SkillTag { SkillTagId = 1001, SkillName = "Paints" },
            new SkillTag { SkillTagId = 1012, SkillName = "Ballet" },
        ];
        var skillIds = skills.Select(s => s.SkillTagId).ToList();
        var dragon = new Dragon
        {
            GivenName = "Fluffy",
            CanBreathFire = true,
            CanTakePassengers = false,
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b",
            SkillTags = skills
        };
        var insertedDragon = new Immutable<Dragon>();
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Insert(It.IsAny<Dragon>()))
            .Callback<Dragon>(insertedDragon.Set);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(skills.Clone());

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, dragon.Clone());

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Dragon>>>();
        insertedDragon.Get().ShouldBeEquivalentTo(dragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null, "GivenName",         "is required")]
    [InlineData("",   "GivenName",         "is required")]
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
            CanBreathFire = true,
            CanTakePassengers = false,
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b"
        };
        typeof(Dragon).GetProperty(expectedFailureField)!.SetValue(dragon, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(It.IsAny<IList<int>>())).Returns([]);

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
            CanBreathFire = false,
            CanTakePassengers = false,
            WeightInKg = -1,
            LengthInMeters = -1,
            FightingSkills = "x"
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(It.IsAny<IList<int>>())).Returns([]);

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new DragonValidationFailures
        {
            GivenName = "is required",
            WeightInKg = "must be a positive number",
            LengthInMeters = "must be a positive number",
            FightingSkills = "must be 'b', 'm', or 'a'"
        });
    }

    [Fact]
    public async Task UpdateDragon_ValidInput_ExpectUpdateOfRecordAndSavesOnce()
    {
        var dragonId = 1;
        List<SkillTag> oldSkills = [
            new SkillTag { SkillTagId = 1001, SkillName = "Paints" },
            new SkillTag { SkillTagId = 1012, SkillName = "Ballet" },
        ];
        List<SkillTag> newSkills = [
            new SkillTag { SkillTagId = 1002, SkillName = "Cartography" },
            new SkillTag { SkillTagId = 1012, SkillName = "Ballet" },
            new SkillTag { SkillTagId = 1020, SkillName = "Swim Coaching" },
        ];        
        var skillIds = newSkills.Select(s => s.SkillTagId).ToList();        
        var existingDragon = new Dragon
        {
            DragonId = dragonId,
            GivenName = "Old Name",
            FamilyName = "Old Family",
            CanBreathFire = false,
            CanTakePassengers = false,
            WeightInKg = 5,
            LengthInMeters = 3,
            FightingSkills = "b",
            SkillTags = oldSkills
        };
        var updatedDragon = new Dragon
        {
            DragonId = dragonId,
            GivenName = "New Name",
            FamilyName = "New Family",
            CanBreathFire = true,
            CanTakePassengers = true,
            WeightInKg = 20,
            LengthInMeters = 10,
            FightingSkills = "a",
            SkillTags = newSkills
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.GetByID(dragonId)).ReturnsAsync(existingDragon);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(newSkills.Clone());

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, dragonId, updatedDragon);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Dragon>>>();
        existingDragon.ShouldBeEquivalentTo(updatedDragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDragon_DragonNotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.GetByID(It.IsAny<object>())).ReturnsAsync((Dragon?)null);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(It.IsAny<IList<int>>())).Returns([]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, 999, new Dragon());

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData(" ",  "GivenName",         "is required")]
    [InlineData(-5,   "WeightInKg",        "must be a positive number")]
    [InlineData(-10,  "LengthInMeters",    "must be a positive number")]
    [InlineData("c",  "FightingSkills",    "must be 'b', 'm', or 'a'")]
    public async Task UpdateDragon_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int DRAGON_ID = 3792;
        var dragon = new Dragon
        {
            GivenName = "Thunderclaw",
            CanBreathFire = true,
            CanTakePassengers = false,
            WeightInKg = 50,
            LengthInMeters = 8,
            FightingSkills = "m"
        };
        typeof(Dragon).GetProperty(expectedFailureField)!.SetValue(dragon, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(dragon);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(It.IsAny<IList<int>>())).Returns([]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, DRAGON_ID, dragon);

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
        const int DRAGON_ID = 278;
        var dragon = new Dragon
        {
            GivenName = " ",
            CanBreathFire = false,
            CanTakePassengers = false,
            WeightInKg = -5,
            LengthInMeters = -10,
            FightingSkills = "c"
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(dragon);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(It.IsAny<IList<int>>())).Returns([]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, DRAGON_ID, dragon);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<DragonValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<DragonValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new DragonValidationFailures
        {
            GivenName = "is required",
            WeightInKg = "must be a positive number",
            LengthInMeters = "must be a positive number",
            FightingSkills = "must be 'b', 'm', or 'a'"
        });
    }

    [Fact]
    public async Task DeleteDragon_DragonExists_ExpectOkAndSavesOnce()
    {
        const int DRAGON_ID = 42;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteDragon_DragonNotFound_ExpectNotFoundAndDoesNotSave()
    {
        const int DRAGON_ID = 999;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.NotFound);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteDragon_DragonHasAssignment_ExpectConflictAndDoesNotSave()
    {
        const int DRAGON_ID = 7;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonHasAnAssignment(DRAGON_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }
}

using DragonCommonApplication.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignmentDomain.Enum;
using DragonAssignmentDomain.Models;
using DragonAssignmentApplication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests;

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
        var inputDragon = new DragonCreateEdit
        {
            GivenName = "Fluffy",
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b",
            SkillTagIds = skillIds
        };
        var expectedDragon = new Dragon
        {
            GivenName = "Fluffy",
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b",
            SkillTags = skills
        };
        var insertedDragon = new Immutable<Dragon>();
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Insert(It.IsAny<Dragon>()))
            .Callback<Dragon>(insertedDragon.Set);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(skills.Clone());

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, inputDragon);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Dragon>>>();
        insertedDragon.Get().ShouldBeEquivalentTo(expectedDragon);
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
        var inputDragon = new DragonCreateEdit
        {
            GivenName = "Fluffy",
            WeightInKg = 10,
            LengthInMeters = 5,
            FightingSkills = "b"
        };
        typeof(DragonCreateEdit).GetProperty(expectedFailureField)!.SetValue(inputDragon, invalidValue);
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, inputDragon);

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
        var inputDragon = new DragonCreateEdit
        {
            GivenName = null!,
            WeightInKg = -1,
            LengthInMeters = -1,
            FightingSkills = "x"
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository).Returns(new Mock<IGenericRepository<Dragon>>().Object);

        //Act
        var response = await DragonEndpoints.CreateDragonAsync(unitOfWorkMock.Object, inputDragon);

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
            WeightInKg = 5,
            LengthInMeters = 3,
            FightingSkills = "b",
            SkillTags = oldSkills
        };
        var inputDragon = new DragonCreateEdit
        {
            GivenName = "New Name",
            FamilyName = "New Family",
            WeightInKg = 20,
            LengthInMeters = 10,
            FightingSkills = "a",
            SkillTagIds = skillIds
        };
        var expectedDragon = new Dragon
        {
            DragonId = dragonId,
            GivenName = "New Name",
            FamilyName = "New Family",
            WeightInKg = 20,
            LengthInMeters = 10,
            FightingSkills = "a",
            SkillTags = newSkills
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetDragonWithJobAsync(dragonId, JobInclusions.None)).ReturnsAsync([existingDragon]);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(newSkills.Clone());

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, dragonId, inputDragon);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Dragon>>>();
        existingDragon.ShouldBeEquivalentTo(expectedDragon);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDragon_DragonNotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetDragonWithJobAsync(It.IsAny<int>(), JobInclusions.None)).ReturnsAsync([]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, 999, new DragonCreateEdit());

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
        var existingDragon = new Dragon
        {
            GivenName = "Thunderclaw",
            WeightInKg = 50,
            LengthInMeters = 8,
            FightingSkills = "m"
        };
        var inputDragon = new DragonCreateEdit
        {
            GivenName = "Thunderclaw",
            WeightInKg = 50,
            LengthInMeters = 8,
            FightingSkills = "m"
        };
        typeof(DragonCreateEdit).GetProperty(expectedFailureField)!.SetValue(inputDragon, invalidValue);
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.GetDragonWithJobAsync(DRAGON_ID, JobInclusions.None)).ReturnsAsync([existingDragon]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, DRAGON_ID, inputDragon);

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
        var existingDragon = new Dragon
        {
            GivenName = "Thunderclaw",
            WeightInKg = 50,
            LengthInMeters = 8,
            FightingSkills = "m"
        };
        var inputDragon = new DragonCreateEdit
        {
            GivenName = " ",
            WeightInKg = -5,
            LengthInMeters = -10,
            FightingSkills = "c"
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.GetDragonWithJobAsync(DRAGON_ID, JobInclusions.None)).ReturnsAsync([existingDragon]);

        //Act
        var response = await DragonEndpoints.UpdateDragonAsync(unitOfWorkMock.Object, DRAGON_ID, inputDragon);

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
}

using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignment.Domain.Models;
using DragonAssignment.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.DragonTests;

public class DragonSelectTests
{
    [Fact]
    public void GetDragons_WithJobIdAndFightingSkill_ExpectFightingSkillPassedThrough()
    {
        const int JOB_ID = 10;
        List<Dragon> dragons = [
            new Dragon { DragonId = 5, GivenName = "Cinder" },
        ];
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetDragonsWithoutOverlappingAssignments(JOB_ID, Array.Empty<int>(), "a"))
            .Returns(dragons);

        //Act
        var response = DragonEndpoints.GetDragons(unitOfWorkMock.Object, [], fightingSkill: "a", jobId: JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<PagedData<Dragon>>>();
        var okResult = (Ok<PagedData<Dragon>>)response.Result;
        okResult.Value!.Data.ShouldBeEquivalentTo(dragons);
    }

    [Fact]
    public void GetDragons_SkillTagIdsWithoutJobId_ExpectBadRequest()
    {
        int[] skillTagIds = [1001];
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();

        //Act
        var response = DragonEndpoints.GetDragons(unitOfWorkMock.Object, skillTagIds);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = (BadRequest<ValidatedResponse>)response.Result;
        badResult.Value!.ValidationFailures.ShouldContain("Filtering by skills is only allowed when a jobId is specified");
    }
}

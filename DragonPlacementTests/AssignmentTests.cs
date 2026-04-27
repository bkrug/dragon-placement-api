using DragonPlacementDataLayer.Repositories;
using DragonPlacementDataLayer.Models;
using DragonPlacementApi.Endpoints;
using Moq;
using Shouldly;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DragonPlacementTests;

public class AssignmentTests
{
    [Fact]
    public async Task DragonIsAssignedToJob_HasNoPreviousSchedules_Success()
    {
        const int DRAGON_ID = 5001;
        const int JOB_ID = 6001;
        Immutable<Assignment> actualAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(new Job { JobTitle = "Commercial Spokesperson" });
        unitOfWorkMock.Setup(m => m.AssignmentRepository.Insert(It.IsAny<Assignment>())).Callback((Assignment a) => actualAssignmentRecord.Set(a));

        //Act
        var response = await AssignmentEndpoints.AssignDragonToJobAsync(unitOfWorkMock.Object, DRAGON_ID, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        actualAssignmentRecord.Get().ShouldBeEquivalentTo(new Assignment
        {
            DragonId = DRAGON_ID,
            JobId = JOB_ID
        });
        unitOfWorkMock.Verify(m => m.SaveAsync(), Times.Once);
    }
}

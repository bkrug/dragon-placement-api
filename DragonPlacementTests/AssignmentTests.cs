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

    [Fact]
    public async Task DragoneIsAssignedToJob_HasNoConflictsWithPreviousSchedules_Success()
    {
        const int DRAGON_ID = 5002;
        const int JOB_ID = 6002;
        Job jobModel = new() { JobTitle = "Commercial Spokesperson", StartDate = DateTime.UtcNow.AddMonths(3), EndDate = DateTime.UtcNow.AddMonths(9) };
        Immutable<Assignment> actualAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.GetDragonAssignments(DRAGON_ID))
            .Returns([
               new Assignment() { StartDate = DateTime.UtcNow.AddMonths(-1), EndDate = DateTime.UtcNow.AddMonths(2) },
               new Assignment() { StartDate = DateTime.UtcNow.AddMonths(10), EndDate = DateTime.UtcNow.AddMonths(14) }
            ]);
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

    [Theory]
    [InlineData(-1, 4)]   // old job that ends after new job starts
    [InlineData(4, 8)]    // old job that is shorter than new job
    [InlineData(2, 10)]   // old job that is longer than new job
    [InlineData(8, 12)]   // old job that starts before new job ends
    public async Task DragoneIsAssignedToJob_ConflictExistsWithPreviousSchedules_Failure(int startInMonths, int endInMonths)
    {
        const int DRAGON_ID = 5003;
        const int JOB_ID = 6003;
        Job jobModel = new() { JobTitle = "Commercial Spokesperson", StartDate = DateTime.UtcNow.AddMonths(3), EndDate = DateTime.UtcNow.AddMonths(9) };
        Immutable<Assignment> actualAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.GetDragonAssignments(DRAGON_ID))
            .Returns([
               new Assignment() { StartDate = DateTime.UtcNow.AddMonths(-10), EndDate = DateTime.UtcNow.AddMonths(-4) },
               new Assignment() { StartDate = DateTime.UtcNow.AddMonths(startInMonths), EndDate = DateTime.UtcNow.AddMonths(endInMonths) },
               new Assignment() { StartDate = DateTime.UtcNow.AddMonths(24), EndDate = DateTime.UtcNow.AddMonths(36) }
            ]);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.Insert(It.IsAny<Assignment>())).Callback((Assignment a) => actualAssignmentRecord.Set(a));

        //Act
        var response = await AssignmentEndpoints.AssignDragonToJobAsync(unitOfWorkMock.Object, DRAGON_ID, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = response.Result as BadRequest<ValidatedResponse>;
        badResult.ShouldBeEquivalentTo(new ValidatedResponse
        {
           IsInternalError = false,
           IsSuccess = false,
           ValidationFailures = [
               "Job overlaps an existing assignment"
           ]
        });
        unitOfWorkMock.Verify(m => m.SaveAsync(), Times.Never);
    }    
}

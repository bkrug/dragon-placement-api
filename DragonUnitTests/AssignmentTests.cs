using DragonAssignment.Application;
using DragonAssignment.Domain.Models;
using DragonPlacementApi.Endpoints;
using Moq;
using Shouldly;
using Microsoft.AspNetCore.Http.HttpResults;
using DragonPlacementApi.Poco;

namespace DragonUnitTests;

//TODO: Add some field to the "Assignment" table to indicate that the worker has already done billable work.
// When this has happened, it is no longer valid to delete the assignment.
// Assert that assignments can only be deleted before billing has occurred, not after.
// This check will remain impossible until some billing module is added to the system.

public class AssignmentTests
{
    [Fact]
    public async Task DragonIsAssignedToJob_HasNoConflictsWithPreviousSchedules_Success()
    {
        const int DRAGON_ID = 5002;
        const int JOB_ID = 6002;
        Job jobModel = new()
        {
            JobId = JOB_ID,
            JobTitle = "Commercial Spokesperson",
            StartDate = DateTime.UtcNow.AddMonths(3),
            EndDate = DateTime.UtcNow.AddMonths(9)
        };
        Immutable<Assignment> actualInsertedAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.GetOverlappingAssignments(DRAGON_ID, jobModel.StartDate, jobModel.EndDate))
            .Returns([]);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.Insert(It.IsAny<Assignment>())).Callback((Assignment a) => actualInsertedAssignmentRecord.Set(a));

        //Act
        var response = await JobEndpoints.AssignDragonToJobAsync(unitOfWorkMock.Object, DRAGON_ID, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        actualInsertedAssignmentRecord.Get().ShouldBeEquivalentTo(new Assignment
        {
            DragonId = DRAGON_ID,
            JobId = JOB_ID,
            StartDate = jobModel.StartDate,
            EndDate = jobModel.EndDate
        });
        unitOfWorkMock.Verify(m => m.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DragonIsAssignedToJob_ConflictExistsWithPreviousSchedules_Failure()
    {
        const int DRAGON_ID = 5003;
        const int JOB_ID = 6003;

        Job jobModel = new()
        {
            JobTitle = "Commercial Spokesperson",
            StartDate = DateTime.UtcNow.AddMonths(3),
            EndDate = DateTime.UtcNow.AddMonths(9)
        };
        Assignment overlappingAssignment = new()
        {
            StartDate = jobModel.StartDate.AddMonths(-1),
            EndDate = jobModel.EndDate.AddMonths(1)
        };

        Immutable<Assignment> actualAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.GetOverlappingAssignments(DRAGON_ID, jobModel.StartDate, jobModel.EndDate))
            .Returns([ overlappingAssignment ]);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.Insert(It.IsAny<Assignment>())).Callback((Assignment a) => actualAssignmentRecord.Set(a));

        //Act
        var response = await JobEndpoints.AssignDragonToJobAsync(unitOfWorkMock.Object, DRAGON_ID, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = (BadRequest<ValidatedResponse>)response.Result;
        var validationMessage = badResult?.Value?.ValidationFailures.Single();
        validationMessage.ShouldStartWith("Overlaps with at least one job");
        unitOfWorkMock.Verify(m => m.SaveChangesAsync(), Times.Never);
    }
}

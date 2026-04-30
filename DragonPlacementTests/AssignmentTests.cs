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
    public async Task DragonIsAssignedToJob_HasNoConflictsWithPreviousSchedules_Success()
    {
        const int DRAGON_ID = 5002;
        const int JOB_ID = 6002;
        Job jobModel = new() { JobTitle = "Commercial Spokesperson" };
        jobModel.SetStartDate(DateTime.UtcNow.AddMonths(3));
        jobModel.SetEndDate(DateTime.UtcNow.AddMonths(9));
        Immutable<Assignment> actualInsertedAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.GetOverlappingAssignments(DRAGON_ID, jobModel.StartDateUnix, jobModel.EndDateUnix))
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
            StartDateUnix = jobModel.StartDateUnix,
            EndDateUnix = jobModel.EndDateUnix
        });
        unitOfWorkMock.Verify(m => m.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DragonIsAssignedToJob_ConflictExistsWithPreviousSchedules_Failure()
    {
        const int DRAGON_ID = 5003;
        const int JOB_ID = 6003;

        Job jobModel = new() { JobTitle = "Commercial Spokesperson" };
        jobModel.SetStartDate(DateTime.UtcNow.AddMonths(3));
        jobModel.SetEndDate(DateTime.UtcNow.AddMonths(9));
        Assignment overlappingAssignment = new ();
        overlappingAssignment.SetStartDate(jobModel.GetStartDate().AddMonths(-1));
        overlappingAssignment.SetEndDate(jobModel.GetEndDate().AddMonths(1));

        Immutable<Assignment> actualAssignmentRecord = new();

        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(new Dragon { DragonId = DRAGON_ID, GivenName = "Fred" });
        unitOfWorkMock.Setup(m => m.JobRepository.GetByID(JOB_ID)).ReturnsAsync(jobModel);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.GetOverlappingAssignments(DRAGON_ID, jobModel.StartDateUnix, jobModel.EndDateUnix))
            .Returns([ overlappingAssignment ]);
        unitOfWorkMock.Setup(m => m.AssignmentRepository.Insert(It.IsAny<Assignment>())).Callback((Assignment a) => actualAssignmentRecord.Set(a));

        //Act
        var response = await JobEndpoints.AssignDragonToJobAsync(unitOfWorkMock.Object, DRAGON_ID, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedResponse>>();
        var badResult = (BadRequest<ValidatedResponse>)response.Result;
        var validationMessage = badResult?.Value?.ValidationFailures.Single();
        validationMessage.ShouldStartWith("Overlaps with at least one job");
        unitOfWorkMock.Verify(m => m.SaveAsync(), Times.Never);
    }
}

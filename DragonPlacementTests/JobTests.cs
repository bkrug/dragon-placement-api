using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonPlacementTests;

public class JobTests
{
    [Fact]
    public async Task CreateJob_ValidInput_ExpectInsertionOfRecordAndSavesOnce()
    {
        List<SkillTag> skills = [
            new SkillTag { SkillTagId = 2001, SkillName = "Fire Safety" },
            new SkillTag { SkillTagId = 2012, SkillName = "Crowd Control" },
        ];
        var skillIds = skills.Select(s => s.SkillTagId).ToList();
        var job = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000,
            SkillTags = skills
        };
        var insertedJob = new Immutable<Job>();
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobRepository.Insert(It.IsAny<Job>()))
            .Callback<Job>(insertedJob.Set);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(skills.Clone());

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, job.Clone());

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Job>>>();
        insertedJob.Get().ShouldBeEquivalentTo(job);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null, "JobTitle",           "is required")]
    [InlineData("",   "JobTitle",           "is required")]
    [InlineData(-1,   "NumberOfPositions",  "must be a positive number")]
    public async Task CreateJob_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var job = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        typeof(Job).GetProperty(expectedFailureField)!.SetValue(job, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.JobRepository).Returns(new Mock<IGenericRepository<Job>>().Object);

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, job);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        var actualMessage = typeof(JobValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task CreateJob_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var job = new Job
        {
            JobTitle = null!,
            NumberOfPositions = -1,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(m => m.JobRepository).Returns(new Mock<IGenericRepository<Job>>().Object);

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, job);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new JobValidationFailures
        {
            JobTitle = "is required",
            NumberOfPositions = "must be a positive number"
        });
    }

    [Fact]
    public async Task UpdateJob_ValidInput_ExpectUpdateOfRecordAndSavesOnce()
    {
        const int JOB_ID = 1;
        List<SkillTag> oldSkills = [
            new SkillTag { SkillTagId = 2001, SkillName = "Fire Safety" },
            new SkillTag { SkillTagId = 2012, SkillName = "Crowd Control" },
        ];
        List<SkillTag> newSkills = [
            new SkillTag { SkillTagId = 2002, SkillName = "Dragon Taming" },
            new SkillTag { SkillTagId = 2012, SkillName = "Crowd Control" },
            new SkillTag { SkillTagId = 2020, SkillName = "Emergency Response" },
        ];
        var skillIds = newSkills.Select(s => s.SkillTagId).ToList();
        var existingJob = new Job
        {
            JobId = JOB_ID,
            JobTitle = "Old Title",
            EmployerName = "Old Employer",
            NumberOfPositions = 1,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000,
            SkillTags = oldSkills
        };
        var updatedJob = new Job
        {
            JobId = JOB_ID,
            JobTitle = "New Title",
            EmployerName = "New Employer",
            NumberOfPositions = 5,
            StartDateUnix = 3000000,
            EndDateUnix = 4000000,
            SkillTags = newSkills
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([existingJob]);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(newSkills.Clone());

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, updatedJob);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Job>>>();
        existingJob.ShouldBeEquivalentTo(updatedJob);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateJob_JobNotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(It.IsAny<int>())).ReturnsAsync([]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, 999, new Job());

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData(" ",  "JobTitle",           "is required")]
    [InlineData(-5,   "NumberOfPositions",  "must be a positive number")]
    public async Task UpdateJob_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int JOB_ID = 3792;
        var job = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        typeof(Job).GetProperty(expectedFailureField)!.SetValue(job, invalidValue);
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([job]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, job);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        var actualMessage = typeof(JobValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task UpdateJob_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        const int JOB_ID = 278;
        var job = new Job
        {
            JobTitle = " ",
            NumberOfPositions = -5,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([job]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, job);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new JobValidationFailures
        {
            JobTitle = "is required",
            NumberOfPositions = "must be a positive number"
        });
    }

    [Fact]
    public async Task DeleteJob_JobExists_ExpectOkAndSavesOnce()
    {
        const int JOB_ID = 42;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobRepository.Delete(JOB_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await JobEndpoints.DeleteJobAsync(unitOfWorkMock.Object, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteJob_JobNotFound_ExpectNotFoundAndDoesNotSave()
    {
        const int JOB_ID = 999;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobRepository.Delete(JOB_ID)).Returns(DeleteResult.NotFound);

        //Act
        var response = await JobEndpoints.DeleteJobAsync(unitOfWorkMock.Object, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteJob_JobHasAssignment_ExpectConflictAndDoesNotSave()
    {
        const int JOB_ID = 7;
        var unitOfWorkMock = new Mock<IAssignmentUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobHasAnAssignment(JOB_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(u => u.JobRepository.Delete(JOB_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await JobEndpoints.DeleteJobAsync(unitOfWorkMock.Object, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }
}

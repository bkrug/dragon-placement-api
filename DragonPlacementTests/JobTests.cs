using CommonDataLayer.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignmentDomain.Models;
using DragonAssignmentApplication;
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
        var inputJob = new JobCreateEdit
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 2 * Const.SECONDS_IN_A_DAY,
            SkillTagIds = skillIds
        };
        var expectedJob = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDate = new DateTime(1970, 1, 2),
            EndDate = new DateTime(1970, 1, 3),
            SkillTags = skills
        };
        var insertedJob = new Immutable<Job>();
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobRepository.Insert(It.IsAny<Job>()))
            .Callback<Job>(insertedJob.Set);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(skills.Clone());

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, inputJob);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Job>>>();
        insertedJob.Get().ShouldBeEquivalentTo(expectedJob);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null,     "JobTitle",           "is required")]
    [InlineData("",       "JobTitle",           "is required")]
    [InlineData(-1,       "NumberOfPositions",  "must be a positive number")]
    [InlineData(1000001L, "StartDateUnix",      "must be midnight UTC")]
    [InlineData(1000001L, "EndDateUnix",        "must be midnight UTC")]
    public async Task CreateJob_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var inputJob = new JobCreateEdit
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 2 * Const.SECONDS_IN_A_DAY
        };
        typeof(JobCreateEdit).GetProperty(expectedFailureField)!.SetValue(inputJob, invalidValue);
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.JobRepository).Returns(new Mock<IGenericRepository<Job>>().Object);

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, inputJob);

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
        var inputJob = new JobCreateEdit
        {
            JobTitle = null!,
            NumberOfPositions = -1,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(m => m.JobRepository).Returns(new Mock<IGenericRepository<Job>>().Object);

        //Act
        var response = await JobEndpoints.CreateJobAsync(unitOfWorkMock.Object, inputJob);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new JobValidationFailures
        {
            JobTitle = "is required",
            NumberOfPositions = "must be a positive number",
            StartDateUnix = "must be midnight UTC",
            EndDateUnix = "must be midnight UTC"
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
            StartDate = new DateTime(1970, 1, 2),
            EndDate = new DateTime(1970, 1, 3),
            SkillTags = oldSkills
        };
        var inputJob = new JobCreateEdit
        {
            JobTitle = "New Title",
            EmployerName = "New Employer",
            NumberOfPositions = 5,
            StartDateUnix = 2592000,
            EndDateUnix = 3456000,
            SkillTagIds = skillIds
        };
        var expectedJob = new Job
        {
            JobId = JOB_ID,
            JobTitle = "New Title",
            EmployerName = "New Employer",
            NumberOfPositions = 5,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(2592000).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(3456000).UtcDateTime,
            SkillTags = newSkills
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([existingJob]);
        unitOfWorkMock.Setup(u => u.GetSkillTagsById(skillIds)).Returns(newSkills.Clone());

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, inputJob);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<Job>>>();
        existingJob.ShouldBeEquivalentTo(expectedJob);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateJob_JobNotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(It.IsAny<int>())).ReturnsAsync([]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, 999, new JobCreateEdit());

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData(" ",      "JobTitle",           "is required")]
    [InlineData(-5,       "NumberOfPositions",  "must be a positive number")]
    [InlineData(1000001L, "StartDateUnix",      "must be midnight UTC")]
    [InlineData(1000001L, "EndDateUnix",        "must be midnight UTC")]
    public async Task UpdateJob_InvalidInput_ExpectBadRequestWithValidationFailure(
        object? invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int JOB_ID = 3792;
        var existingJob = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDate = new DateTime(1970, 1, 2),
            EndDate = new DateTime(1970, 1, 3)
        };
        var inputJob = new JobCreateEdit
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 2 * Const.SECONDS_IN_A_DAY
        };
        typeof(JobCreateEdit).GetProperty(expectedFailureField)!.SetValue(inputJob, invalidValue);
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([existingJob]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, inputJob);

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
        var existingJob = new Job
        {
            JobTitle = "Dragon Wrangler",
            EmployerName = "Dragonscale Inc.",
            NumberOfPositions = 3,
            StartDate = new DateTime(1970, 1, 2),
            EndDate = new DateTime(1970, 1, 3)
        };
        var inputJob = new JobCreateEdit
        {
            JobTitle = " ",
            NumberOfPositions = -5,
            StartDateUnix = 1000000,
            EndDateUnix = 2000000
        };
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetJobWithSkillsAsync(JOB_ID)).ReturnsAsync([existingJob]);

        //Act
        var response = await JobEndpoints.UpdateJobAsync(unitOfWorkMock.Object, JOB_ID, inputJob);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<JobValidationFailures>>>();
        var badResult = (BadRequest<ValidatedForm<JobValidationFailures>>)response.Result;
        var failures = badResult.Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new JobValidationFailures
        {
            JobTitle = "is required",
            NumberOfPositions = "must be a positive number",
            StartDateUnix = "must be midnight UTC",
            EndDateUnix = "must be midnight UTC"
        });
    }

    [Fact]
    public async Task DeleteJob_JobExists_ExpectOkAndSavesOnce()
    {
        const int JOB_ID = 42;
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
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
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
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
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.JobHasAnAssignment(JOB_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(u => u.JobRepository.Delete(JOB_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await JobEndpoints.DeleteJobAsync(unitOfWorkMock.Object, JOB_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }
}

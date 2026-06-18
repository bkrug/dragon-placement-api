using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonPlacementTests;

public class HoursWorkedTests
{
    private const long START_UNIX = 1_000_000;
    private const long VALID_END_UNIX = START_UNIX + 3_600; // 1 hour later

    [Fact]
    public async Task CreateHoursWorked_ValidInput_ExpectInsertionAndSavesOnce()
    {
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = VALID_END_UNIX
        };
        var expectedEntry = new HoursWorked
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = VALID_END_UNIX
        };
        var insertedEntry = new Immutable<HoursWorked>();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.HoursWorkedRepository.Insert(It.IsAny<HoursWorked>()))
            .Callback<HoursWorked>(insertedEntry.Set);

        // Act
        var response = await HoursWorkedEndpoints.CreateHoursWorkedAsync(unitOfWorkMock.Object, input);

        // Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<HoursWorked>>>();
        insertedEntry.Get().ShouldBeEquivalentTo(expectedEntry);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateHoursWorked_ValidInput_ExpectUpdateAndSavesOnce()
    {
        const int HOURS_WORKED_ID = 55;
        var existingEntry = new HoursWorked
        {
            HoursWorkedId = HOURS_WORKED_ID,
            AssignmentId = 5,
            DragonId = 15,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = VALID_END_UNIX
        };
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 7,
            DragonId = 17,
            StartDateTimeUnix = START_UNIX + 7_200,
            EndDateTimeUnix = START_UNIX + 14_400
        };
        var expectedEntry = new HoursWorked
        {
            HoursWorkedId = HOURS_WORKED_ID,
            AssignmentId = 7,
            DragonId = 17,
            StartDateTimeUnix = START_UNIX + 7_200,
            EndDateTimeUnix = START_UNIX + 14_400
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.HoursWorkedRepository.GetByID(HOURS_WORKED_ID)).ReturnsAsync(existingEntry);

        // Act
        var response = await HoursWorkedEndpoints.UpdateHoursWorkedAsync(unitOfWorkMock.Object, HOURS_WORKED_ID, input);

        // Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<HoursWorked>>>();
        existingEntry.ShouldBeEquivalentTo(expectedEntry);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateHoursWorked_EndBeforeStart_ExpectBadRequestWithEndDateFailure()
    {
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = START_UNIX - 1
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.HoursWorkedRepository).Returns(new Mock<IGenericRepository<HoursWorked>>().Object);

        // Act
        var response = await HoursWorkedEndpoints.CreateHoursWorkedAsync(unitOfWorkMock.Object, input);

        // Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<HoursWorkedValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new HoursWorkedValidationFailures
        {
            StartDateTimeUnix = string.Empty,
            EndDateTimeUnix = "Cannot clock out before clocking in"
        });
    }

    [Fact]
    public async Task UpdateHoursWorked_EndBeforeStart_ExpectBadRequestWithEndDateFailure()
    {
        const int HOURS_WORKED_ID = 55;
        var existingEntry = new HoursWorked
        {
            HoursWorkedId = HOURS_WORKED_ID,
            AssignmentId = 5,
            DragonId = 15,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = VALID_END_UNIX
        };
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = START_UNIX - 1
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.HoursWorkedRepository.GetByID(HOURS_WORKED_ID)).ReturnsAsync(existingEntry);

        // Act
        var response = await HoursWorkedEndpoints.UpdateHoursWorkedAsync(unitOfWorkMock.Object, HOURS_WORKED_ID, input);

        // Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<HoursWorkedValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new HoursWorkedValidationFailures
        {
            StartDateTimeUnix = string.Empty,
            EndDateTimeUnix = "Cannot clock out before clocking in"
        });
    }

    [Theory]
    [InlineData(30L)]      // below 60-second minimum
    [InlineData(86_401L)]  // above 24-hour maximum
    public async Task CreateHoursWorked_DurationOutOfRange_ExpectBadRequestWithEndDateFailure(long durationSeconds)
    {
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = START_UNIX + durationSeconds
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.HoursWorkedRepository).Returns(new Mock<IGenericRepository<HoursWorked>>().Object);

        // Act
        var response = await HoursWorkedEndpoints.CreateHoursWorkedAsync(unitOfWorkMock.Object, input);

        // Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<HoursWorkedValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new HoursWorkedValidationFailures
        {
            StartDateTimeUnix = string.Empty,
            EndDateTimeUnix = "Cannot log less than 1 minute or more than 24 hours as a single log entry"
        });
    }

    [Theory]
    [InlineData(30L)]      // below 60-second minimum
    [InlineData(86_401L)]  // above 24-hour maximum
    public async Task UpdateHoursWorked_DurationOutOfRange_ExpectBadRequestWithEndDateFailure(long durationSeconds)
    {
        const int HOURS_WORKED_ID = 55;
        var existingEntry = new HoursWorked
        {
            HoursWorkedId = HOURS_WORKED_ID,
            AssignmentId = 5,
            DragonId = 15,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = VALID_END_UNIX
        };
        var input = new HoursWorkedCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateTimeUnix = START_UNIX,
            EndDateTimeUnix = START_UNIX + durationSeconds
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.HoursWorkedRepository.GetByID(HOURS_WORKED_ID)).ReturnsAsync(existingEntry);

        // Act
        var response = await HoursWorkedEndpoints.UpdateHoursWorkedAsync(unitOfWorkMock.Object, HOURS_WORKED_ID, input);

        // Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<HoursWorkedValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<HoursWorkedValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new HoursWorkedValidationFailures
        {
            StartDateTimeUnix = string.Empty,
            EndDateTimeUnix = "Cannot log less than 1 minute or more than 24 hours as a single log entry"
        });
    }
}

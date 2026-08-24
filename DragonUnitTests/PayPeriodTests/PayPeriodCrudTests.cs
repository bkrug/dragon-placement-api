using DragonCommon.Application.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;
using DragonTimekeeping.Domain.Models;
using DragonTimekeeping.Application;
using DragonTimekeeping.Application.PayPeriodUpsert;
using DragonCommon.Domain.Poco;
using DragonTimekeeping.Domain.Enums;

namespace DragonUnitTests.PayPeriodTests;

public class PayPeriodCrudTests
{
    [Fact]
    public async Task CreatePayPeriod_ValidInput_ExpectInsertionAndSavesOnce()
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .WithAssignmentId(1827)
            .WithDragonId(382)
            .AddHoursWorked("1970-01-05T09:00:00", "1970-01-05T17:00:00")
            .Build();
        var expectedPayPeriod = new PayPeriodBuilder()
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 11))
            .WithAssignmentId(1827)
            .WithSubmissionStatus(PayPeriodStatus.Draft) //This should be the status of all pay periods upon initial creation.
            .AddHoursWorked(new DateTime(1970, 1, 5, 9, 0, 0), new DateTime(1970, 1, 5, 17, 0, 0))
            .Build();
        var insertedPayPeriod = new Immutable<PayPeriod>();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Insert(It.IsAny<PayPeriod>()))
            .Callback<PayPeriod>(insertedPayPeriod.Set);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        insertedPayPeriod.Get().ShouldBeEquivalentTo(expectedPayPeriod);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("1970-01-05T05:00:00", "StartDate",  "StartDate",  "must exclude time-of-day or be midnight UTC")]
    [InlineData("Juabary 5th 70ly",    "StartDate",  "StartDate",  "must be an ISO Date")]
    [InlineData("1970-01-06",          "StartDate",  "StartDate",  "must be a Monday")]
    [InlineData("1970-01-11T05:00:00", "EndDate",    "EndDate",    "must exclude time-of-day or be midnight UTC")]
    [InlineData("Juabayr 11th dl",     "EndDate",    "EndDate",    "must be an ISO Date")]
    [InlineData("1970-01-09",          "EndDate",    "EndDate",    "must be a Sunday")]
    [InlineData("1970-01-04",          "EndDate",    "EndDate",    "must be greater than StartDate")]
    public async Task CreatePayPeriod_InvalidInput_ExpectBadRequestWithValidationFailure(
        string invalidValue,
        string inputField,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .Build();
        typeof(PayPeriodCreateEdit).GetProperty(inputField)!.SetValue(input, invalidValue);
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures[expectedFailureField].ShouldBe(expectedFailureMessage);
    }

    [Theory]
    [InlineData("",                    "StartDateTime",  "is required")]
    [InlineData("Juabary 5th 70ly",    "StartDateTime",  "must be an ISO Date")]
    [InlineData("",                    "EndDateTime",    "is required")]
    [InlineData("Juabayr 11th dl",     "EndDateTime",    "must be an ISO Date")]
    public async Task CreatePayPeriod_InvalidHoursWorkedInput_ExpectBadRequestWithValidationFailure(
        string invalidValue,
        string fieldName,
        string expectedFailureMessage)
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-06T09:00", "1970-01-06T16:00")
            .Build();
        typeof(HoursWorkedCreateEdit).GetProperty(fieldName)!.SetValue(input.HoursWorked[0], invalidValue);
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.GridRowFailures["HoursWorked"][0].FieldFailures[fieldName].ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task CreatePayPeriod_HoursWorkedStartBeforePayPeriod_ExpectBadRequest()
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-04T23:59:59", "1970-01-05T01:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEmpty();
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 0,
                RowValidationMessage = "Clock-in time is outside of the pay period"
            }
        });
    }

    [Fact]
    public async Task CreatePayPeriod_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-05T20:00:00", "1970-01-06T02:00:00")
            .AddHoursWorked("1970-01-06T20:00:00", "1970-01-07T02:00:00")
            .AddHoursWorked("1970-01-11T23:00:00", "1970-01-12T01:00:00") //Invalid
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEmpty();
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 2,
                RowValidationMessage = "Clock-out time is outside of the pay period"
            }
        });
    }

    [Fact]
    public async Task CreatePayPeriod_TwoHoursWorkedOverlap_ExpectBadRequestWithOverlapFailures()
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-05T09:00:00", "1970-01-05T12:00:00")
            .AddHoursWorked("1970-01-06T09:00:00", "1970-01-06T14:00:00")
            .AddHoursWorked("1970-01-06T13:00:00", "1970-01-06T17:00:00")
            .AddHoursWorked("1970-01-07T09:00:00", "1970-01-07T12:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEmpty();
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 1,
                RowValidationMessage = "Overlaps with another hours-worked record"
            },
            new() {
                Index = 2,
                RowValidationMessage = "Overlaps with another hours-worked record"
            }
        });
    }

    [Fact]
    public async Task CreatePayPeriod_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05T13:46:41")
            .WithEndDate("1970-01-09")
            .AddHoursWorked("1970-01-01T00:00:00", "1970-04-15T10:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEquivalentTo(new Dictionary<string, string>
        {
            { "StartDate", "must exclude time-of-day or be midnight UTC" },
            { "EndDate", "must be a Sunday" }
        });
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 0,
                RowValidationMessage = "Clock-in time is outside of the pay period"
            }
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_ValidInput_ExpectUpdateAndSavesOnce()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithStartDate(new DateTime(1970, 1, 12))
            .WithEndDate(new DateTime(1970, 1, 18))
            .WithSubmissionStatus(PayPeriodStatus.Draft)
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithAssignmentId(5)
            .WithDragonId(15)
            .WithStartDate("1970-01-12")
            .WithEndDate("1970-01-18")
            .AddHoursWorked("1970-01-12T09:00:00", "1970-01-12T17:00:00")
            .Build();
        var expectedEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithStartDate(new DateTime(1970, 1, 12))
            .WithEndDate(new DateTime(1970, 1, 18))
            .WithSubmissionStatus(PayPeriodStatus.Draft)
            .AddHoursWorked(new DateTime(1970, 1, 12, 9, 0, 0), new DateTime(1970, 1, 12, 17, 0, 0))
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.ShouldBeEquivalentTo(expectedEntry);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePayPeriod_InputReplacesOneRecordAndDropsAnother_ExpectOnlyInputRecordsRemain()
    {
        const int PAY_PERIOD_ID = 55;
        DateTime MONDAY_START = new (1970, 1, 5);
        DateTime TUESDAY_START = new (1970, 1, 6);
        DateTime WEDNESDAY_START = new (1970, 1, 7);
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 11))
            .AddHoursWorked(301, MONDAY_START, MONDAY_START.AddHours(1))
            .AddHoursWorked(302, TUESDAY_START, TUESDAY_START.AddHours(1))
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-05T00:00:00", "1970-01-05T02:00:00")
            .AddHoursWorked("1970-01-07T00:00:00", "1970-01-07T01:00:00")
            .Build();
        var expectedHoursWorked = new List<HoursWorked>
        {
            new()
            {
                HoursWorkedId = 301,
                StartDateTime = MONDAY_START,
                EndDateTime = MONDAY_START.AddHours(2)
            },
            new()
            {
                StartDateTime = WEDNESDAY_START,
                EndDateTime = WEDNESDAY_START.AddHours(1)
            }
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.HoursWorked.ShouldBeEquivalentTo(expectedHoursWorked);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePayPeriod_NotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(It.IsAny<int>())).ReturnsAsync((PayPeriod?)null);
        var input = new PayPeriodCreateEditBuilder().Build();

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, 999, input);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("1970-01-05T05:00:00", "StartDate",  "StartDate",  "must exclude time-of-day or be midnight UTC")]
    [InlineData("Juabary 5th 70ly",    "StartDate",  "StartDate",  "must be an ISO Date")]
    [InlineData("1970-01-06",          "StartDate",  "StartDate",  "must be a Monday")]
    [InlineData("1970-01-11T05:00:00", "EndDate",    "EndDate",    "must exclude time-of-day or be midnight UTC")]
    [InlineData("Juabary 11o 70ly",    "EndDate",    "EndDate",    "must be an ISO Date")]
    [InlineData("1970-01-10",          "EndDate",    "EndDate",    "must be a Sunday")]
    [InlineData("1970-01-04",          "EndDate",    "EndDate",    "must be greater than StartDate")]
    public async Task UpdatePayPeriod_InvalidInput_ExpectBadRequestWithValidationFailure(
        string invalidValue,
        string inputField,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 11))
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .Build();
        typeof(PayPeriodCreateEdit).GetProperty(inputField)!.SetValue(input, invalidValue);
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures[expectedFailureField].ShouldBe(expectedFailureMessage);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(PayPeriodStatus.Submitted)]
    [InlineData(PayPeriodStatus.Billed)]
    public async Task UpdatePayPeriod_NotDraftStatus_ExpectBadRequestWithModelLevelFailure(PayPeriodStatus existingStatus)
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 11))
            .WithSubmissionStatus(existingStatus)
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("not a real date") //Intentionally malformed, to prove the status check wins before field validation runs.
            .WithEndDate("1970-01-11")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ModelLevelFailure.ShouldBe("Cannot edit a pay period unless it is in status 'Draft'");
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePayPeriod_HoursWorkedStartBeforePayPeriod_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 1))
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-06T11:00:00", "1970-01-06T13:00:00")
            .AddHoursWorked("1970-01-04T23:09:59", "1970-01-05T01:00:00") //Invalid Record
            .AddHoursWorked("1970-01-07T11:00:00", "1970-01-07T13:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEmpty();
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 1,
                RowValidationMessage = "Clock-in time is outside of the pay period"
            }
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 5))
            .WithEndDate(new DateTime(1970, 1, 11))
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-05")
            .WithEndDate("1970-01-11")
            .AddHoursWorked("1970-01-05T09:00:00", "1970-01-05T17:00:00")
            .AddHoursWorked("1970-01-06T09:00:00", "1970-01-05T13:00:00")
            .AddHoursWorked("1970-01-06T14:00:00", "1970-01-05T18:00:00")
            .AddHoursWorked("1970-01-11T23:00:00", "1970-01-12T02:00:00") //Invalid
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEmpty();
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 3,
                RowValidationMessage = "Clock-out time is outside of the pay period"
            }
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithStartDate("1970-01-12T13:46:41")
            .WithEndDate("1970-01-18T03:33:21")
            .AddHoursWorked("1970-01-01T00:00:00", "1970-04-15T10:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<ValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<ValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.FieldFailures.ShouldBeEquivalentTo(new Dictionary<string, string>
        {
            { "StartDate", "must exclude time-of-day or be midnight UTC" },
            { "EndDate", "must exclude time-of-day or be midnight UTC" }
        });
        failures.GridRowFailures["HoursWorked"].ShouldBeEquivalentTo(new List<GridRowValidationFailures>
        {
            new() {
                Index = 0,
                RowValidationMessage = "Clock-in time is outside of the pay period"
            }
        });
    }
}

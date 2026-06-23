using CommonDataLayer.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignmentDomain.Models;
using DragonAssignmentApplication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;
using DragonTimekeepingDomain.Models;
using DragonTimekeepingDomain.Validation;
using DragonTimekeepingApplication;
using DragonTimekeepingApplication.Dto;

namespace DragonPlacementTests.PayPeriodTests;

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
            .WithStartDateUnix(4, Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(10, Const.SECONDS_IN_A_DAY)
            .WithAssignmentId(1827)
            .AddHoursWorkedRelative(clockInSeconds: 9 * 3600, clockOutSeconds: 17 * 3600)
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
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        var actualMessage = typeof(PayPeriodValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 0,
                    RowValidationMessage = "Clock-in time is outside of the pay period"
                }
            ]
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
                failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 2,
                    RowValidationMessage = "Clock-out time is outside of the pay period"
                }
            ]
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 1,
                    RowValidationMessage = "Overlaps with another hours-worked record"
                },
                new HoursWorkedValidationFailures {
                    Index = 2,
                    RowValidationMessage = "Overlaps with another hours-worked record"
                }
            ]
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            StartDate = "must exclude time-of-day or be midnight UTC",
            EndDate = "must be a Sunday",
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 0,
                    RowValidationMessage = "Clock-in time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_ValidInput_ExpectUpdateAndSavesOnce()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithStartDateUnix(11 * Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(17 * Const.SECONDS_IN_A_DAY)
            .WithSubmissionStatus("Draft")
            .Build();
        var input = new PayPeriodCreateEditBuilder()
            .WithAssignmentId(5)
            .WithDragonId(15)
            .WithStartDate("1970-01-12")
            .WithEndDate("1970-01-18")
            .AddHoursWorked("1970-01-12T09:00:00", "1970-01-12T17:00:00")
            .WithSubmissionStatus("Submitted")
            .Build();
        var expectedEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithStartDateUnix(11 * Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(17 * Const.SECONDS_IN_A_DAY)
            .WithSubmissionStatus("Submitted")
            .AddHoursWorked(11 * Const.SECONDS_IN_A_DAY + 9 * Const.SECONDS_IN_AN_HOUR, 11 * Const.SECONDS_IN_A_DAY + 17 * Const.SECONDS_IN_AN_HOUR)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.ShouldBeEquivalentTo(expectedEntry);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePayPeriod_InputReplacesOneRecordAndDropsAnother_ExpectOnlyInputRecordsRemain()
    {
        const int PAY_PERIOD_ID = 55;
        const long MONDAY_START = 4 * Const.SECONDS_IN_A_DAY;
        const long TUESDAY_START = 5 * Const.SECONDS_IN_A_DAY;
        const long WEDNESDAY_START = 6 * Const.SECONDS_IN_A_DAY;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDateUnix(4, Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(10, Const.SECONDS_IN_A_DAY)
            .AddHoursWorked(301, MONDAY_START, MONDAY_START + 3600)
            .AddHoursWorked(302, TUESDAY_START, TUESDAY_START + 3600)        
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
                StartDateTimeUnix = MONDAY_START,
                EndDateTimeUnix = MONDAY_START + 7200
            },
            new()
            {
                StartDateTimeUnix = WEDNESDAY_START,
                EndDateTimeUnix = WEDNESDAY_START + 3600
            }
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.HoursWorked.ShouldBeEquivalentTo(expectedHoursWorked);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
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
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
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
            .WithStartDateUnix(4, Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(10, Const.SECONDS_IN_A_DAY)
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        var actualMessage = typeof(PayPeriodValidationFailures)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task UpdatePayPeriod_HoursWorkedStartBeforePayPeriod_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDateUnix(4, Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(0, Const.SECONDS_IN_A_DAY)
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 1,
                    RowValidationMessage = "Clock-in time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDateUnix(4, Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(10, Const.SECONDS_IN_A_DAY)
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 3,
                    RowValidationMessage = "Clock-out time is outside of the pay period"
                }
            ]
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
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            StartDate = "must exclude time-of-day or be midnight UTC",
            EndDate = "must exclude time-of-day or be midnight UTC",
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    Index = 0,
                    RowValidationMessage = "Clock-in time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task DeletePayPeriod_Exists_ExpectOkAndSavesOnce()
    {
        const int PAY_PERIOD_ID = 42;
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Delete(PAY_PERIOD_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await PayPeriodEndpoints.DeletePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePayPeriod_NotFound_ExpectNotFoundAndDoesNotSave()
    {
        const int PAY_PERIOD_ID = 999;
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Delete(PAY_PERIOD_ID)).Returns(DeleteResult.NotFound);

        //Act
        var response = await PayPeriodEndpoints.DeletePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPayPeriodNewAsync_PayPeriodWithHoursWorked_ExpectTransformedView()
    {
        const int PAY_PERIOD_ID = 77;
        const int DRAGON_ID = 20;
        const int ASSIGNMENT_ID = 10;
        const int JOB_ID = 30;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDateUnix(1 * Const.SECONDS_IN_A_DAY)
            .WithEndDateUnix(7 * Const.SECONDS_IN_A_DAY)
            .AddHoursWorkedRelative(401, 32400, 61200)
            .AddHoursWorkedRelative(402, 1 * Const.SECONDS_IN_A_DAY + 32400, 1 * Const.SECONDS_IN_A_DAY + 61200)
            .Build();
        var dragon = new Dragon { DragonId = DRAGON_ID, GivenName = "Smaug", FamilyName = "the Terrible" };
        var job = new Job { JobId = JOB_ID, JobTitle = "Guard", EmployerName = "Castle Corp" };
        var assignment = new Assignment
        {
            AssignmentId = ASSIGNMENT_ID, DragonId = DRAGON_ID, JobId = JOB_ID,
            Dragon = dragon, Job = job
        };
        var timekeepingMock = new Mock<ITimekeepingUnitOfWork>();
        timekeepingMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);
        var assignmentMock = new Mock<IDragonPlacementUnitOfWork>();
        assignmentMock.Setup(u => u.GetAssignmentWithDragonAndJobAsync(ASSIGNMENT_ID)).ReturnsAsync(assignment);

        //Act
        var response = await PayPeriodEndpoints.GetPayPeriodAsync(
            timekeepingMock.Object, assignmentMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriodView>>>();
        var payload = ((Ok<ValidatedPayload<PayPeriodView>>)response.Result).Value!.Payload;
        payload.ShouldBeEquivalentTo(new PayPeriodView
        {
            AssignmentId = ASSIGNMENT_ID,
            StartDate = "1970-01-02",
            EndDate = "1970-01-08",
            SubmissionStatus = "Draft",
            DragonName = "Smaug the Terrible",
            AssignmentDescription = "Guard at Castle Corp",
            HoursWorked =
            [
                new HoursWorkedView
                {
                    StartDateTime = "1970-01-02T09:00:00",
                    EndDateTime = "1970-01-02T17:00:00"
                },
                new HoursWorkedView
                {
                    StartDateTime = "1970-01-03T09:00:00",
                    EndDateTime = "1970-01-03T17:00:00"
                }
            ]
        });
    }

}

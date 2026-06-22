using CommonDataLayer.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TimekeepingDataLayer.Models;
using TimekeepingDataLayer.Repositories;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodCrudTests
{
    private static PayPeriodCreateEdit MakeValidInput() => new()
    {
        AssignmentId = 10,
        DragonId = 20,
        StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
        EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
        SubmissionStatus = "Draft",
        HoursWorked =
        [
            new HoursWorkedCreateEdit
            {
                StartDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY,
                EndDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 3600
            }
        ]
    };

    [Fact]
    public async Task CreatePayPeriod_ValidInput_ExpectInsertionAndSavesOnce()
    {
        var input = MakeValidInput();
        var expectedPayPeriod = new PayPeriodBuilder()
            .AddHoursWorked(clockInSeconds: 0, clockOutSeconds: 3600)
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
    [InlineData(1000001L, "StartDateUnix",  "must be midnight UTC")]
    [InlineData(1000001L, "EndDateUnix",    "must be midnight UTC")]
    [InlineData(86400L,   "EndDateUnix",    "must be greater than StartDateUnix")]
    public async Task CreatePayPeriod_InvalidInput_ExpectBadRequestWithValidationFailure(
        long invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var input = MakeValidInput();
        typeof(PayPeriodCreateEdit).GetProperty(expectedFailureField)!.SetValue(input, invalidValue);
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
        var input = MakeValidInput();
        input.HoursWorked =
        [
            new HoursWorkedCreateEdit
            {
                StartDateTimeUnix = input.StartDateUnix - 1,
                EndDateTimeUnix = input.StartDateUnix + 3600
            }
        ];
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        //TODO: Someday this validation failure should be on a child model.
        //TODO: Assert the actual validation message.
        failures.HoursWorkedStartDateTimeUnix.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreatePayPeriod_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        var input = MakeValidInput();
        input.HoursWorked =
        [
            new HoursWorkedCreateEdit
            {
                StartDateTimeUnix = input.EndDateUnix + Const.SECONDS_IN_A_DAY - 3600,
                EndDateTimeUnix = input.EndDateUnix + Const.SECONDS_IN_A_DAY
            }
        ];
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        //TODO: Someday this validation failure should be on a child model.
        //TODO: Assert the actual validation message.
        failures.HoursWorkedEndDateTimeUnix.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreatePayPeriod_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var input = new PayPeriodCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1000001,
            EndDateUnix = 2000001,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorkedCreateEdit
                {
                    StartDateTimeUnix = 0,
                    EndDateTimeUnix = 9_000_000
                }
            ]
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            StartDateUnix = "must be midnight UTC",
            EndDateUnix = "must be midnight UTC",
            //TODO: Someday these two validation failure should be on a child model.
            HoursWorkedStartDateTimeUnix = "all must be greater than or equal to pay period StartDateUnix",
            HoursWorkedEndDateTimeUnix = "all must be less than pay period EndDateUnix plus one day"
        });
    }

    [Fact]
    public async Task UpdatePayPeriod_ValidInput_ExpectUpdateAndSavesOnce()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithDragonId(15)
            .Build();
        var input = MakeValidInput();
        input.SubmissionStatus = "Submitted";
        var expectedEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus("Submitted")
            .AddHoursWorked(clockInSeconds: 0, clockOutSeconds: 3600)
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
        const long MONDAY_START = 1 * Const.SECONDS_IN_A_DAY;
        const long TUESDAY_START = 2 * Const.SECONDS_IN_A_DAY;
        const long WEDNESDAY_START = 3 * Const.SECONDS_IN_A_DAY;
        var existingEntry = new PayPeriodBuilder().WithPayPeriodId(PAY_PERIOD_ID)
            .AddHoursWorked(301, 0, 3600)
            .AddHoursWorked(302, 1 * Const.SECONDS_IN_A_DAY, 1 * Const.SECONDS_IN_A_DAY + 3600)        
            .Build();
        var input = new PayPeriodCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorkedCreateEdit
                {
                    StartDateTimeUnix = MONDAY_START,
                    EndDateTimeUnix = MONDAY_START + 7200
                },
                new HoursWorkedCreateEdit
                {
                    StartDateTimeUnix = WEDNESDAY_START,
                    EndDateTimeUnix = WEDNESDAY_START + 3600
                }
            ]
        };
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

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, 999, MakeValidInput());

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData(1000001L, "StartDateUnix",  "must be midnight UTC")]
    [InlineData(1000001L, "EndDateUnix",    "must be midnight UTC")]
    [InlineData(86400L,   "EndDateUnix",    "must be greater than StartDateUnix")]
    public async Task UpdatePayPeriod_InvalidInput_ExpectBadRequestWithValidationFailure(
        long invalidValue,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = MakeValidInput();
        typeof(PayPeriodCreateEdit).GetProperty(expectedFailureField)!.SetValue(input, invalidValue);
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
            .Build();
        var input = MakeValidInput();
        input.HoursWorked =
        [
            new HoursWorkedCreateEdit
            {
                StartDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY - 1,
                EndDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 3600
            }
        ];
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        //TODO: Someday this validation failure should be on a child model.
        //TODO: Assert the actual validation message.
        failures.HoursWorkedStartDateTimeUnix.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdatePayPeriod_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = MakeValidInput();
        input.HoursWorked =
        [
            new HoursWorkedCreateEdit
            {
                StartDateTimeUnix = 8 * Const.SECONDS_IN_A_DAY,
                EndDateTimeUnix = 9 * Const.SECONDS_IN_A_DAY
            }
        ];
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        //TODO: Someday this validation failure should be on a child model.
        //TODO: Assert the actual validation message.
        failures.HoursWorkedEndDateTimeUnix.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdatePayPeriod_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEdit
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1000001,
            EndDateUnix = 2000001,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorkedCreateEdit
                {
                    StartDateTimeUnix = 0,
                    EndDateTimeUnix = 9_000_000
                }
            ]
        };
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailures>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailures>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailures
        {
            StartDateUnix = "must be midnight UTC",
            EndDateUnix = "must be midnight UTC",
            //TODO: Someday this validation failure should be on a child model.
            HoursWorkedStartDateTimeUnix = "all must be greater than or equal to pay period StartDateUnix",
            HoursWorkedEndDateTimeUnix = "all must be less than pay period EndDateUnix plus one day"
        });
    }

    [Fact]
    public async Task CreatePayPeriodNew_ValidInput_ExpectInsertionAndSavesOnce()
    {
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T01:00:00")
            .Build();
        var expectedPayPeriod = new PayPeriodBuilder()
            .AddHoursWorked(clockInSeconds: 0, clockOutSeconds: 3600)
            .Build();
        var insertedPayPeriod = new Immutable<PayPeriod>();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.PayPeriodRepository.Insert(It.IsAny<PayPeriod>()))
            .Callback<PayPeriod>(insertedPayPeriod.Set);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodNewAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        insertedPayPeriod.Get().ShouldBeEquivalentTo(expectedPayPeriod);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Theory]
    [InlineData("1970-01-02T05:00:00", "StartDate",  "StartDate",  "must exclude time-of-day or be midnight UTC")]
    [InlineData("Januar 1, 1970",      "StartDate",  "StartDate",  "must be an ISO Date")]
    [InlineData("1970-01-02T05:00:00", "EndDate",    "EndDate",    "must exclude time-of-day or be midnight UTC")]
    [InlineData("Januar 2, 1970",      "EndDate",    "EndDate",    "must be an ISO Date")]
    [InlineData("1970-01-02",          "EndDate",    "EndDate",    "must be greater than StartDate")]
    public async Task CreatePayPeriodNew_InvalidInput_ExpectBadRequestWithValidationFailure(
        string invalidValue,
        string inputField,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T01:00:00")
            .Build();
        typeof(PayPeriodCreateEditNew).GetProperty(inputField)!.SetValue(input, invalidValue);
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodNewAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        var actualMessage = typeof(PayPeriodValidationFailuresNew)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task CreatePayPeriodNew_HoursWorkedStartBeforePayPeriod_ExpectBadRequest()
    {
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-01T23:59:59", "1970-01-02T01:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodNewAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    StartDateTime = "Clock-in time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task CreatePayPeriodNew_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-09T23:00:00", "1970-01-10T00:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodNewAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
                failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    EndDateTime = "Clock-out time is outside of the pay period"
                }
            ]
        });

    }

    [Fact]
    public async Task CreatePayPeriodNew_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        var input = new PayPeriodCreateEditNewBuilder()
            .WithStartDate("1970-01-12T13:46:41")
            .WithEndDate("1970-01-24T03:33:21")
            .AddHoursWorked("1970-01-01T00:00:00", "1970-04-15T10:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(m => m.PayPeriodRepository).Returns(new Mock<IGenericRepository<PayPeriod>>().Object);

        //Act
        var response = await PayPeriodEndpoints.CreatePayPeriodNewAsync(unitOfWorkMock.Object, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            StartDate = "must exclude time-of-day or be midnight UTC",
            EndDate = "must exclude time-of-day or be midnight UTC",
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    StartDateTime = "Clock-in time is outside of the pay period",
                    EndDateTime = "Clock-out time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task UpdatePayPeriodNew_ValidInput_ExpectUpdateAndSavesOnce()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithAssignmentId(5)
            .WithDragonId(15)
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T01:00:00")
            .WithSubmissionStatus("Submitted")
            .Build();
        var expectedEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithSubmissionStatus("Submitted")
            .AddHoursWorked(clockInSeconds: 0, clockOutSeconds: 3600)
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.ShouldBeEquivalentTo(expectedEntry);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePayPeriodNew_InputReplacesOneRecordAndDropsAnother_ExpectOnlyInputRecordsRemain()
    {
        const int PAY_PERIOD_ID = 55;
        const long MONDAY_START = 1 * Const.SECONDS_IN_A_DAY;
        const long WEDNESDAY_START = 3 * Const.SECONDS_IN_A_DAY;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .AddHoursWorked(301, 0, 3600)
            .AddHoursWorked(302, 1 * Const.SECONDS_IN_A_DAY, 1 * Const.SECONDS_IN_A_DAY + 3600)        
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T02:00:00")
            .AddHoursWorked("1970-01-04T00:00:00", "1970-01-04T01:00:00")
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
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriod>>>();
        existingEntry.HoursWorked.ShouldBeEquivalentTo(expectedHoursWorked);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePayPeriodNew_NotFound_ExpectNotFoundAndDoesNotSave()
    {
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(It.IsAny<int>())).ReturnsAsync((PayPeriod?)null);
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T01:00:00")
            .Build();

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, 999, input);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Theory]
    [InlineData("1970-01-02T05:00:00", "StartDate",  "StartDate",  "must exclude time-of-day or be midnight UTC")]
    [InlineData("January 1, 1970",     "StartDate",  "StartDate",  "must be an ISO Date")]
    [InlineData("1970-01-02T05:00:00", "EndDate",    "EndDate",    "must exclude time-of-day or be midnight UTC")]
    [InlineData("January 2, 1970",     "EndDate",    "EndDate",    "must be an ISO Date")]
    [InlineData("1970-01-02",          "EndDate",    "EndDate",    "must be greater than StartDate")]
    public async Task UpdatePayPeriodNew_InvalidInput_ExpectBadRequestWithValidationFailure(
        string invalidValue,
        string inputField,
        string expectedFailureField,
        string expectedFailureMessage)
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-02T00:00:00", "1970-01-02T01:00:00")
            .Build();
        typeof(PayPeriodCreateEditNew).GetProperty(inputField)!.SetValue(input, invalidValue);
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        var actualMessage = typeof(PayPeriodValidationFailuresNew)
            .GetProperty(expectedFailureField)!
            .GetValue(failures) as string;
        actualMessage.ShouldBe(expectedFailureMessage);
    }

    [Fact]
    public async Task UpdatePayPeriodNew_HoursWorkedStartBeforePayPeriod_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-01T23:59:59", "1970-01-02T01:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    StartDateTime = "Clock-in time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task UpdatePayPeriodNew_HoursWorkedEndAfterPayPeriodPlusOneDay_ExpectBadRequest()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .AddHoursWorked("1970-01-09T23:00:00", "1970-01-10T00:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    EndDateTime = "Clock-out time is outside of the pay period"
                }
            ]
        });
    }

    [Fact]
    public async Task UpdatePayPeriodNew_AllFieldsInvalid_ExpectBadRequestWithAllValidationFailures()
    {
        const int PAY_PERIOD_ID = 55;
        var existingEntry = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .Build();
        var input = new PayPeriodCreateEditNewBuilder()
            .WithStartDate("1970-01-12T13:46:41")
            .WithEndDate("1970-01-24T03:33:21")
            .AddHoursWorked("1970-01-01T00:00:00", "1970-04-15T10:00:00")
            .Build();
        var unitOfWorkMock = new Mock<ITimekeepingUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(existingEntry);

        //Act
        var response = await PayPeriodEndpoints.UpdatePayPeriodNewAsync(unitOfWorkMock.Object, PAY_PERIOD_ID, input);

        //Assert
        response.Result.ShouldBeOfType<BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>>();
        var failures = ((BadRequest<ValidatedForm<PayPeriodValidationFailuresNew>>)response.Result).Value!.ValidationFailures;
        failures.ShouldBeEquivalentTo(new PayPeriodValidationFailuresNew
        {
            StartDate = "must exclude time-of-day or be midnight UTC",
            EndDate = "must exclude time-of-day or be midnight UTC",
            HoursWorked = [
                new HoursWorkedValidationFailures {
                    StartDateTime = "Clock-in time is outside of the pay period",
                    EndDateTime = "Clock-out time is outside of the pay period"
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
            .AddHoursWorked(401, 32400, 61200)
            .AddHoursWorked(402, 1 * Const.SECONDS_IN_A_DAY + 32400, 1 * Const.SECONDS_IN_A_DAY + 61200)
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
        var response = await PayPeriodEndpoints.GetPayPeriodNewAsync(
            timekeepingMock.Object, assignmentMock.Object, PAY_PERIOD_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedPayload<PayPeriodView>>>();
        var payload = ((Ok<ValidatedPayload<PayPeriodView>>)response.Result).Value!.Payload;
        payload.ShouldBeEquivalentTo(new PayPeriodView
        {
            AssignmentId = ASSIGNMENT_ID,
            DragonId = DRAGON_ID,
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

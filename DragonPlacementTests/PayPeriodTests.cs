using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonPlacementDataLayer.Models;
using DragonPlacementDataLayer.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonPlacementTests;

public class PayPeriodTests
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
        var expectedPayPeriod = new PayPeriod
        {
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorked
                {
                    StartDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY,
                    EndDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 3600
                }
            ]
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 5,
            DragonId = 15,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft"
        };
        var input = MakeValidInput();
        input.SubmissionStatus = "Submitted";
        var expectedEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Submitted",
            HoursWorked =
            [
                new HoursWorked
                {
                    StartDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY,
                    EndDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 3600
                }
            ]
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorked
                {
                    HoursWorkedId = 301,
                    StartDateTimeUnix = MONDAY_START,
                    EndDateTimeUnix = MONDAY_START + 3600
                },
                new HoursWorked
                {
                    HoursWorkedId = 302,
                    StartDateTimeUnix = TUESDAY_START,
                    EndDateTimeUnix = TUESDAY_START + 3600
                }
            ]
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft"
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft"
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft"
        };
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
        var existingEntry = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = 10,
            DragonId = 20,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 8 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft"
        };
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
        var payPeriod = new PayPeriod
        {
            PayPeriodId = PAY_PERIOD_ID,
            AssignmentId = ASSIGNMENT_ID,
            DragonId = DRAGON_ID,
            StartDateUnix = 1 * Const.SECONDS_IN_A_DAY,
            EndDateUnix = 7 * Const.SECONDS_IN_A_DAY,
            SubmissionStatus = "Draft",
            HoursWorked =
            [
                new HoursWorked
                {
                    HoursWorkedId = 401,
                    StartDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 32400,
                    EndDateTimeUnix = 1 * Const.SECONDS_IN_A_DAY + 61200
                },
                new HoursWorked
                {
                    HoursWorkedId = 402,
                    StartDateTimeUnix = 2 * Const.SECONDS_IN_A_DAY + 32400,
                    EndDateTimeUnix = 2 * Const.SECONDS_IN_A_DAY + 61200
                }
            ]
        };
        var dragon = new Dragon { DragonId = DRAGON_ID, GivenName = "Smaug", FamilyName = "the Terrible" };
        var job = new Job { JobId = JOB_ID, JobTitle = "Guard", EmployerName = "Castle Corp" };
        var assignment = new Assignment
        {
            AssignmentId = ASSIGNMENT_ID, DragonId = DRAGON_ID, JobId = JOB_ID, Job = job
        };
        var timekeepingMock = new Mock<ITimekeepingUnitOfWork>();
        timekeepingMock.Setup(u => u.GetPayPeriodWithHoursWorkedAsync(PAY_PERIOD_ID)).ReturnsAsync(payPeriod);
        var assignmentMock = new Mock<IAssignmentUnitOfWork>();
        assignmentMock.Setup(u => u.DragonRepository.GetByID(DRAGON_ID)).ReturnsAsync(dragon);
        assignmentMock.Setup(u => u.AssignmentRepository.GetByID(ASSIGNMENT_ID)).ReturnsAsync(assignment);

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

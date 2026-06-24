using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignmentDomain.Models;
using DragonAssignmentApplication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;
using DragonTimekeepingApplication;

namespace DragonPlacementTests.PayPeriodTests;

public class PayPeriodReadTests
{

    [Fact]
    public async Task GetPayPeriodAsync_PayPeriodWithHoursWorked_ExpectTransformedView()
    {
        const int PAY_PERIOD_ID = 77;
        const int DRAGON_ID = 20;
        const int ASSIGNMENT_ID = 10;
        const int JOB_ID = 30;
        var payPeriod = new PayPeriodBuilder()
            .WithPayPeriodId(PAY_PERIOD_ID)
            .WithStartDate(new DateTime(1970, 1, 2))
            .WithEndDate(new DateTime(1970, 1, 8))
            .AddHoursWorked(401, new DateTime(1970, 1, 2, 9, 0, 0), new DateTime(1970, 1, 2, 17, 0, 0))
            .AddHoursWorked(402, new DateTime(1970, 1, 3, 9, 0, 0), new DateTime(1970, 1, 3, 17, 0, 0))
            .Build();
        var dragon = new Dragon { DragonId = DRAGON_ID, GivenName = "Smaug", FamilyName = "the Terrible" };
        var job = new Job { JobId = JOB_ID, JobTitle = "Guard", EmployerName = "Castle Corp" };
        var assignment = new Assignment
        {
            AssignmentId = ASSIGNMENT_ID,
            DragonId = DRAGON_ID,
            JobId = JOB_ID,
            Dragon = dragon,
            Job = job
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
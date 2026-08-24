namespace DragonBilling.Domain.Enum;

public enum WorkRequestStatus
{
    Unspecified = 0,
    Draft,     // Our employees have recorded the request, but we have not begun looking for worker
    Approved,  // Ready to begin recruiting dragon. A Job object has been created in the other domain.
    Completed  // No further work or billable item are expected for thi work requet.
}
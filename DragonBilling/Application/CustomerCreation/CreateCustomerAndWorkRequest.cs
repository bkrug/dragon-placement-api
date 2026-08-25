namespace DragonBilling.Application.CustomerCreation;

/// <summary>
/// A new customer's first work request is created in the same call as the customer,
/// so this is a flat representation of the Customer and WorkRequest domain models combined.
/// </summary>
public class CreateCustomerAndWorkRequest
{
    public string CustomerName { get; set; } = null!;
    public string WorkRequestName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string EstimatedStartDate { get; set; } = string.Empty;
    public string EstimatedEndDate { get; set; } = string.Empty;
    public int EstimatedWorkforceSize { get; set; }
}

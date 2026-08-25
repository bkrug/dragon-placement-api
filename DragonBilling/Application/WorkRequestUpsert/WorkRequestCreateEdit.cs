namespace DragonBilling.Application.WorkRequestUpsert;

public class WorkRequestCreateEdit
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string EstimatedStartDate { get; set; } = string.Empty;
    public string EstimatedEndDate { get; set; } = string.Empty;
    public int EstimatedWorkforceSize { get; set; }
}

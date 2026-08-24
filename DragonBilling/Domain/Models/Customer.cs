namespace DragonBilling.Domain.Models;

public partial class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<WorkRequest> WorkRequests { get; set; } = [];
}

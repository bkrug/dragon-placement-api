namespace DragonTimekeepingDomain.Models;

public partial class PayPeriod
{
    public int PayPeriodId { get; set; }
    public int AssignmentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string SubmissionStatus { get; set; } = null!;

    public virtual ICollection<HoursWorked> HoursWorked { get; set; } = [];

    public void ApplyEdit(PayPeriod input)
    {
        var inputClockIns = input.HoursWorked.ToList();

        //Delete child records not found in input object
        var clockInsToDelete = HoursWorked
            .Where(existingHw => !inputClockIns.Any(ih => ih.StartDateTime == existingHw.StartDateTime))
            .ToList();
        foreach (var recToDelete in clockInsToDelete)
            HoursWorked.Remove(recToDelete);

        //Update fields in this object
        AssignmentId = input.AssignmentId;
        StartDate = input.StartDate;
        EndDate = input.EndDate;
        SubmissionStatus = input.SubmissionStatus;

        //Insert and update child records coming from input object.
        foreach (var inputClockIn in inputClockIns)
        {
            var existingClockPunch = HoursWorked.FirstOrDefault(h => h.StartDateTime == inputClockIn.StartDateTime);
            if (existingClockPunch == null)
                HoursWorked.Add(inputClockIn);
            else
                existingClockPunch.EndDateTime = inputClockIn.EndDateTime;
        }
    }
}

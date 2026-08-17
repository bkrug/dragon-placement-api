namespace DragonCommon.Domain.Poco;

public class ValidationFailures
{
    public Dictionary<string, string> FieldFailures { get; set; } = [];
    public Dictionary<string, List<GridRowValidationFailures>> GridRowFailures { get; set; } = [];
}

public class GridRowValidationFailures : ValidationFailures
{
    public int Index { get;set; } = -1;
    public string RowValidationMessage { get; set; } = string.Empty;
}

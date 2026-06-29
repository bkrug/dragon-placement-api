namespace DragonCommonDomain.Poco;

public class GridRowValidationFailures
{
    public int Index { get;set; } = -1;
    public string RowValidationMessage { get; set; } = string.Empty;
}

public class ValidationFailures
{
    public Dictionary<string, string> FieldFailures { get; set; }
}
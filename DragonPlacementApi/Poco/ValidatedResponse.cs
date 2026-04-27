namespace DragonPlacementApi.Endpoints;

public class ValidatedResponse
{
    public bool IsSuccess {get;set;}
    public bool IsInternalError {get;set;}
    public IList<string> ValidationFailures {get;set;} = [];
}

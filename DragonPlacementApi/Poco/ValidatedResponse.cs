namespace DragonPlacementApi.Endpoints;

public class ValidatedResponse
{
    public bool IsSuccess {get;set;}
    public bool IsInternalError {get;set;}
    public IList<string> ValidationFailures {get;set;} = [];

    public static ValidatedResponse Success => new ValidatedResponse { IsSuccess = true };
    public static ValidatedResponse NotFound => new ValidatedResponse { ValidationFailures = [ "Not Found"] };
    public static ValidatedResponse ExpectedOneFoundMultiple => new ValidatedResponse { IsInternalError = true, ValidationFailures = [ "Expected exactly one result, but found multiple."] };
}

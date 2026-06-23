namespace DragonPlacementApi.Poco;

public class ValidatedResponse
{
    public bool IsSuccess {get;set;}
    public bool IsInternalError {get;set;}
    public IList<string> ValidationFailures {get;set;} = [];

    public static ValidatedResponse Success => new() { IsSuccess = true };
    public static ValidatedResponse NotFound => new() { ValidationFailures = [ "Not Found"] };
    public static ValidatedResponse ExpectedOneFoundMultiple => new() { IsInternalError = true, ValidationFailures = [ "Expected exactly one result, but found multiple."] };
}

public class ValidatedPayload<T> : ValidatedResponse where T : new()
{
    public T Payload {get;set;} = new T();
    public static ValidatedPayload<T> FromPayload(T payload) =>
        new()
        {
            IsSuccess = true,
            IsInternalError = false,
            ValidationFailures = [],
            Payload = payload
        };
}

public class ValidatedForm<T> where T : new()
{
    public bool IsSuccess {get;set;}
    public bool IsInternalError {get;set;}
    public T ValidationFailures {get;set;} = new T();
}

public class PagedData<T> where T : class
{
    public int Offset {get;set;}
    public int Limit {get;set;}
    public int TotalRecords {get;set;}
    public IList<T> Data {get;set;} = [];
}
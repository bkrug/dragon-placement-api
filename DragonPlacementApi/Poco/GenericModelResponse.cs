namespace DragonPlacementApi.Endpoints;

public class GenericModelResponse<T> where T : class
{
    public bool IsSuccess {get;set;}
    public bool IsInternalError {get;set;}
    public T? ValidationFailures {get;set;}
}

namespace DragonPlacementApi.Endpoints;

public class PagedData<T> where T : class
{
    public int Offset {get;set;}
    public int Limit {get;set;}
    public int TotalRecords {get;set;}
    public IList<T> Data {get;set;} = [];
}

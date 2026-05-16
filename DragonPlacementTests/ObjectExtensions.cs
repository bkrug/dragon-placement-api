using Newtonsoft.Json;

namespace DragonPlacementTests;

public static class ObjectExtensions
{
    public static T Clone<T>(this T source) where T : new()
    {
        var jsonString = JsonConvert.SerializeObject(source) ?? "{}";
        return JsonConvert.DeserializeObject<T>(jsonString) ?? throw new NullReferenceException($"Was not able to clone object of type {typeof(T).Name}");
    }
}
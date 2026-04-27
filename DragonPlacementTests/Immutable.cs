using System.Reflection;

namespace DragonPlacementTests;

public class ImmutabilityException(string message) : Exception(message)
{
}

public class Immutable<T>
{
    private bool isAssigned = false;
    private T? _value;
    public void Set(T value)
    {
        if (isAssigned) {
            throw new ImmutabilityException($"An attempt was made to change an immutable value of type {typeof(T).Name}");
        }
        else
        {
            isAssigned = true;
            _value = value;
        }
    }
    public T? Get()
    {
        return _value;
    }
}
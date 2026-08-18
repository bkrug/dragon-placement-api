using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace DragonUnitTests.ArchitectureTests;

internal static class LayeringTestHelpers
{
    public static string DependencyFailureMessage(TestResult result)
    {
        var typeNameString = result.FailingTypeNames == null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return "A dependency is breaking the clean architecture paradigm: " + typeNameString;
    }
}

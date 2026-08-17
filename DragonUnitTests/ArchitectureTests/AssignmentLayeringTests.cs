using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using TestResult = NetArchTest.Rules.TestResult;

namespace DragonUnitTests.ArchitectureTests;

// DragonAssignment merges Domain/Application/Data into one project, so the compiler can no
// longer stop a layer from referencing the wrong direction (there's no project-reference
// boundary between them). These tests are the enforcement mechanism that replaces it.
public class AssignmentLayeringTests
{
    private static readonly Assembly AssignmentAssembly = typeof(DragonAssignment.Domain.Models.Dragon).Assembly;

    [Fact]
    public void Domain_ShouldNot_DependOn_ApplicationOrData()
    {
        var result = Types.InAssembly(AssignmentAssembly)
            .That().ResideInNamespace("DragonAssignment.Domain")
            .ShouldNot().HaveDependencyOnAny("DragonAssignment.Application", "DragonAssignment.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Data()
    {
        var result = Types.InAssembly(AssignmentAssembly)
            .That().ResideInNamespace("DragonAssignment.Application")
            .ShouldNot().HaveDependencyOn("DragonAssignment.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    private static string DependencyFailureMessage(TestResult result) {
        var typeNameString = result.FailingTypeNames == null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return "A namespace is dependent on a namespace that it should be independent of: " + typeNameString;
    }
}

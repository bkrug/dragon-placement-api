using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using TestResult = NetArchTest.Rules.TestResult;

namespace DragonUnitTests.ArchitectureTests;

// The DragonAssignment project contains three "clean architecture" layers,
// so the compiler can not prevent layer references in the wrong direction.
// These tests enforce the domain layer to be independent,
// and the application layer to be independent of the infrastructure (data) layer.
public class AssignmentLayeringTests
{
    private static readonly Assembly AssignmentAssembly = typeof(DragonAssignment.Domain.Models.Dragon).Assembly;

    [Fact]
    public void Domain_ShouldNot_DependOn_ApplicationOrData()
    {
        var result = Types.InAssembly(AssignmentAssembly)
            .That().ResideInNamespace("DragonAssignment.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "DragonAssignment.Application",
                "DragonAssignment.Data",
                "DragonCommonApplication",
                "DragonCommon.Application",
                "DragonCommonDataLayer",
                "DragonCommon.DataLayer",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Data()
    {
        var result = Types.InAssembly(AssignmentAssembly)
            .That().ResideInNamespace("DragonAssignment.Application")
            .ShouldNot().HaveDependencyOnAny(
                "DragonAssignment.Data",
                "DragonCommonDataLayer",
                "DragonCommon.DataLayer",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    private static string DependencyFailureMessage(TestResult result) {
        var typeNameString = result.FailingTypeNames == null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        return "A dependency is breaking the clean architecture paradigm: " + typeNameString;
    }
}

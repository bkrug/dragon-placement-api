using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using static DragonUnitTests.ArchitectureTests.LayeringTestHelpers;

namespace DragonUnitTests.ArchitectureTests;

// The DragonCommon project contains three "clean architecture" layers,
// so the compiler can not prevent layer references in the wrong direction.
// These tests enforce the domain layer to be independent,
// and the application layer to be independent of the infrastructure (data) layer.
public class LayeringCommonTests
{
    private static readonly Assembly CommonAssembly = typeof(DragonCommon.Domain.ValidationMessages).Assembly;

    [Fact]
    public void Domain_ShouldNot_DependOn_ApplicationOrData()
    {
        var result = Types.InAssembly(CommonAssembly)
            .That().ResideInNamespace("DragonCommon.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "DragonCommon.Application",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Data()
    {
        var result = Types.InAssembly(CommonAssembly)
            .That().ResideInNamespace("DragonCommon.Application")
            .ShouldNot().HaveDependencyOn("DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }
}

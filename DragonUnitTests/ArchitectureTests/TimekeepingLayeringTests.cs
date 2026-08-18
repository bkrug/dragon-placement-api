using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using static DragonUnitTests.ArchitectureTests.LayeringTestHelpers;

namespace DragonUnitTests.ArchitectureTests;

// The DragonTimekeeping project contains three "clean architecture" layers,
// so the compiler can not prevent layer references in the wrong direction.
// These tests enforce the domain layer to be independent,
// and the application layer to be independent of the infrastructure (data) layer.
public class TimekeepingLayeringTests
{
    private static readonly Assembly TimekeepingAssembly = typeof(DragonTimekeeping.Domain.Models.PayPeriod).Assembly;

    [Fact]
    public void Domain_ShouldNot_DependOn_ApplicationOrData()
    {
        var result = Types.InAssembly(TimekeepingAssembly)
            .That().ResideInNamespace("DragonTimekeeping.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "DragonTimekeeping.Application",
                "DragonTimekeeping.Data",
                "DragonCommon.Application",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Data()
    {
        var result = Types.InAssembly(TimekeepingAssembly)
            .That().ResideInNamespace("DragonTimekeeping.Application")
            .ShouldNot().HaveDependencyOnAny(
                "DragonTimekeeping.Data",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }
}

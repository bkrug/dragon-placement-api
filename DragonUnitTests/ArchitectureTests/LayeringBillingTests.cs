using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using static DragonUnitTests.ArchitectureTests.LayeringTestHelpers;

namespace DragonUnitTests.ArchitectureTests;

public class LayeringBillingTests
{
    private static readonly Assembly BillingAssembly = typeof(DragonBilling.Domain.Models.BillableHours).Assembly;

    [Fact]
    public void Domain_ShouldNot_DependOn_ApplicationOrData()
    {
        var result = Types.InAssembly(BillingAssembly)
            .That().ResideInNamespace("DragonBilling.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "DragonBilling.Application",
                "DragonBilling.Data",
                "DragonCommon.Application",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Data()
    {
        var result = Types.InAssembly(BillingAssembly)
            .That().ResideInNamespace("DragonBilling.Application")
            .ShouldNot().HaveDependencyOnAny(
                "DragonBilling.Data",
                "DragonCommon.Data")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }
}
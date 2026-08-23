using System.Reflection;
using DragonAssignment.Domain.Models;
using DragonBilling.Domain.Models;
using DragonTimekeeping.Domain.Models;
using NetArchTest.Rules;
using Shouldly;
using static DragonUnitTests.ArchitectureTests.LayeringTestHelpers;

namespace DragonUnitTests.ArchitectureTests;

public class DomainIndependenceTests
{
    // Each of the projects in DomainAssemblies has its own business domain.
    // None of them should reference another.
    // If you ever need to write an endpoint that deals with more than one domain,
    // then use the display layer to piece respones the domains together.
    // See GetPayPeriodAsync() for an example.
    private static readonly Dictionary<string, Assembly> DomainAssemblies = new()
    {
        ["DragonAssignment"] = typeof(Dragon).Assembly,
        ["DragonBilling"] = typeof(ChargeRate).Assembly,
        ["DragonTimekeeping"] = typeof(PayPeriod).Assembly,
    };

    public static IEnumerable<object[]> Domains
        => DomainAssemblies.Keys.Select(domainNamespace => new object[] { domainNamespace });

    [Theory]
    [MemberData(nameof(Domains))]
    public void Domain_ShouldNot_DependOn_OtherDomains(string domainNamespace)
    {
        var otherDomainNamespaces = DomainAssemblies.Keys
            .Where(name => name != domainNamespace)
            .ToArray();

        var result = Types.InAssembly(DomainAssemblies[domainNamespace])
            .That().ResideInNamespace(domainNamespace)
            .ShouldNot().HaveDependencyOnAny(otherDomainNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(DependencyFailureMessage(result));
    }
}

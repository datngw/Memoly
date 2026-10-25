using System.Reflection;
using FluentAssertions;
using Memoly.Api.Shared.Database;
using NetArchTest.Rules;

namespace Memoly.ArchitectureTests;

public class SharedPurityTests
{
    private static readonly Assembly ApiAssembly = typeof(MemolyDbContext).Assembly;

    [Fact]
    public void Shared_Types_DoNotDependOnFeatures()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("Memoly.Api.Shared")
            .ShouldNot().HaveDependencyOnAny("Memoly.Api.Features")
            .GetTypes();

        result.Should().BeEmpty("Shared/ is building blocks only — it must not know about features");
    }
}

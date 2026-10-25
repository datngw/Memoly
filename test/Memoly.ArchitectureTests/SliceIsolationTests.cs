using System.Reflection;
using FluentAssertions;
using Memoly.Api.Shared.Database;
using NetArchTest.Rules;

namespace Memoly.ArchitectureTests;

public class SliceIsolationTests
{
    private static readonly Assembly ApiAssembly = typeof(MemolyDbContext).Assembly;

    public static IEnumerable<object[]> FeatureNamespaces()
    {
        var namespaces = ApiAssembly.GetTypes()
            .Select(t => t.Namespace ?? string.Empty)
            .Where(n => n.StartsWith("Memoly.Api.Features.", StringComparison.Ordinal))
            .Select(n => string.Join('.', n.Split('.').Take(4)))
            .Where(n => n.Split('.').Length == 4)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);

        return namespaces.Select(n => new object[] { n });
    }

    [Theory]
    [MemberData(nameof(FeatureNamespaces))]
    public void Feature_Types_DoNotDependOnOtherFeatures(string featureNamespace)
    {
        var otherFeatures = FeatureNamespaces()
            .Select(r => (string)r[0])
            .Where(n => n != featureNamespace)
            .ToArray();

        // A single feature has nothing to be isolated from; the rule grows with features.
        if (otherFeatures.Length == 0)
        {
            return;
        }

        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace(featureNamespace)
            .ShouldNot().HaveDependencyOnAny(otherFeatures)
            .GetTypes();

        result.Should().BeEmpty($"slices in {featureNamespace} must not reference other features");
    }
}

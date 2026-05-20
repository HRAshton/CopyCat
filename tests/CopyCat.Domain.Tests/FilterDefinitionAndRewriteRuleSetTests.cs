using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Tests;

public sealed class FilterDefinitionAndRewriteRuleSetTests
{
    [Fact]
    public void AllowAll_ReturnsAllowPolicyWithoutRoot()
    {
        FilterSetDefinition definition = FilterSetDefinition.AllowAll();

        Assert.Equal("Allow all", definition.Name);
        Assert.Equal(MappingDefaultPolicy.Allow, definition.DefaultPolicy);
        Assert.Null(definition.Root);
    }

    [Fact]
    public void EffectiveOperations_WhenOperationsAreNull_ReturnsEmptyCollection()
    {
        RewriteRuleSet rules = new();

        Assert.Empty(rules.EffectiveOperations);
    }

    [Fact]
    public void EffectiveOperations_WhenOperationsExist_ReturnsSameSequence()
    {
        RewriteOperation[] operations =
        [
            new RemoveLinksOperation(),
            new AppendFooterOperation("Footer"),
        ];
        RewriteRuleSet rules = new(operations);

        Assert.Equal(operations, rules.EffectiveOperations);
    }
}

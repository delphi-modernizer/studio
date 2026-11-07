using System;
using System.Linq;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Modules.Traceability.Domain;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Traceability.Application.Tests;

public sealed class ProblemWordingTests
{
    [Fact]
    public void Every_trace_rule_in_the_kit_has_words_and_no_code_shows()
    {
        var rules = KitBundle.Embedded.Spec.TraceRules.Rules.Select(r => r.Id);

        Assert.Equal(rules.Order(), ProblemWordings.Rules.Order());
        Assert.All(rules, rule => {
            var wording = ProblemWordings.Of(rule);
            Assert.DoesNotContain("TR0", wording.Label, StringComparison.Ordinal);
            Assert.DoesNotContain("TR0", wording.Description, StringComparison.Ordinal);
        });
    }
}

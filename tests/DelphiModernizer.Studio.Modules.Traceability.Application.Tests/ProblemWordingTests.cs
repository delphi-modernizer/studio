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

    [Fact]
    public void A_problem_row_reads_its_severity_the_problem_and_where_it_is()
    {
        var wording = ProblemWordings.Of("TR004");

        string name = ProblemWordings.Announced(
            "Error",
            wording.Description,
            wording.Label,
            "business rule BR-001",
            "business-rules.md",
            12);

        Assert.Equal(
            "Error: A business rule points to code that doesn’t exist: a missing file, or lines past its end. "
            + "Broken evidence link, business rule BR-001. business-rules.md, line 12",
            name);
    }

    [Fact]
    public void A_problem_with_no_record_or_line_reads_its_label_and_file()
    {
        string name = ProblemWordings.Announced("Warning", "It is open.", "Open question", null, "questions.md", 0);

        Assert.Equal("Warning: It is open. Open question. questions.md", name);
    }
}

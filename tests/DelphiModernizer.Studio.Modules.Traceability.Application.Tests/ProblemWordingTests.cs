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

    [Theory]
    [InlineData(new string[0], "Last check 14:12")]
    [InlineData(
        new[] { "docs/02-requirements/business-rules.md", "docs/02-requirements/business-rules.md" },
        "In the business rules · last check 14:12")]
    [InlineData(new[] { "docs/a/D03-test-plan.md" }, "In the test plan · last check 14:12")]
    [InlineData(new[] { "docs/a/one.md", "docs/b/two.md" }, "In 2 documents · last check 14:12")]
    public void The_errors_card_says_where_the_errors_are(string[] paths, string expected)
    {
        Assert.Equal(expected, ProblemWordings.ErrorsWhere(paths, "14:12"));
    }

    [Fact]
    public void While_warnings_count_the_errors_card_splits_the_errors_from_the_warnings()
    {
        Assert.Equal("2 errors + 5 warnings", ProblemWordings.StrictErrors(7, 5));
        Assert.Equal("1 error + 1 warning", ProblemWordings.StrictErrors(2, 1));
    }

    [Theory]
    [InlineData(2, 5, "2 + 5")]
    [InlineData(7, 0, "7")]
    [InlineData(0, 5, "5")]
    [InlineData(0, 0, "0")]
    public void An_issue_count_adds_warnings_to_errors_only_when_both(int errors, int warnings, string text)
    {
        Assert.Equal(text, ProblemWordings.IssueCount(errors, warnings));
    }
}

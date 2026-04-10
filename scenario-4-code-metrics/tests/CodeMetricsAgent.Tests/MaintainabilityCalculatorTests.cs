namespace CodeMetricsAgent.Tests;

public class MaintainabilityCalculatorTests
{
    [Fact]
    public void NullInput_ReturnsError()
    {
        var result = MaintainabilityCalculator.CalculateMaintainability(null!);
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void EmptyInput_ReturnsError()
    {
        var result = MaintainabilityCalculator.CalculateMaintainability("");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void WhitespaceInput_ReturnsError()
    {
        var result = MaintainabilityCalculator.CalculateMaintainability("   ");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void SimpleOneLiner_HighMaintainability()
    {
        var code = "var x = 1;";
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        // Simple code should score high (>80)
        Assert.Contains("Maintainability Index:", result);
        Assert.Contains("Highly Maintainable", result);
    }

    [Fact]
    public void ComplexCodeWithManyBranches_LowerMaintainability()
    {
        // Generate complex code with many branches and operators
        var lines = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            lines.Add($"if (x{i} > 0 && y{i} < 100 || z{i} != 0) {{ result{i} = x{i} * y{i} + z{i}; }}");
        }
        var code = string.Join("\n", lines);
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        // Complex code should not be "Highly Maintainable"
        Assert.DoesNotContain("Highly Maintainable", result);
    }

    [Fact]
    public void MaintainabilityScore_BetweenZeroAndHundred()
    {
        var code = """
            public void DoWork()
            {
                var x = 1;
                if (x > 0)
                {
                    Console.WriteLine(x);
                }
            }
            """;
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        // Extract the MI score from the output
        var match = System.Text.RegularExpressions.Regex.Match(result, @"Maintainability Index: ([\d.]+)/100");
        Assert.True(match.Success, "Should contain MI score");
        var score = double.Parse(match.Groups[1].Value);
        Assert.InRange(score, 0.0, 100.0);
    }

    [Fact]
    public void Output_ContainsAllComponents()
    {
        var code = "var x = 1;\nvar y = 2;";
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        Assert.Contains("Maintainability Index:", result);
        Assert.Contains("Rating:", result);
        Assert.Contains("Halstead Volume:", result);
        Assert.Contains("Cyclomatic Complexity:", result);
        Assert.Contains("Lines of Code:", result);
        Assert.Contains("Interpretation:", result);
    }

    [Fact]
    public void Output_ContainsInterpretationGuide()
    {
        var code = "var x = 1;";
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        Assert.Contains("80–100: Highly maintainable", result);
        Assert.Contains("50–79:  Moderate", result);
        Assert.Contains("20–49:  Difficult", result);
        Assert.Contains("0–19:   Unmaintainable", result);
    }

    [Theory]
    [InlineData("var x = 1;", "Highly Maintainable")]
    public void SimpleCode_RatingMatchesExpected(string code, string expectedRating)
    {
        var result = MaintainabilityCalculator.CalculateMaintainability(code);
        Assert.Contains(expectedRating, result);
    }

    [Fact]
    public void LinesOfCode_ReportedCorrectly()
    {
        // 3 non-blank lines
        var code = "var x = 1;\n\nvar y = 2;\n\nvar z = 3;";
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        Assert.Contains("Lines of Code: 3", result);
    }

    [Fact]
    public void CyclomaticComplexity_ReportedInOutput()
    {
        var code = """
            if (x > 0)
            {
                while (y > 0)
                {
                    y--;
                }
            }
            """;
        var result = MaintainabilityCalculator.CalculateMaintainability(code);

        // if + while = 2 branches, base 1 → CC=3
        Assert.Contains("Cyclomatic Complexity: 3", result);
    }

    [Fact]
    public void MoreComplexCode_HasLowerScore()
    {
        var simple = "var x = 1;";
        var complex = string.Join("\n",
            Enumerable.Range(0, 30).Select(i =>
                $"if (x{i} > 0 && y{i} < 100) {{ result += x{i}; }}"));

        var simpleResult = MaintainabilityCalculator.CalculateMaintainability(simple);
        var complexResult = MaintainabilityCalculator.CalculateMaintainability(complex);

        // Extract scores
        var simpleMatch = System.Text.RegularExpressions.Regex.Match(simpleResult, @"Maintainability Index: ([\d.]+)/100");
        var complexMatch = System.Text.RegularExpressions.Regex.Match(complexResult, @"Maintainability Index: ([\d.]+)/100");

        Assert.True(simpleMatch.Success);
        Assert.True(complexMatch.Success);

        var simpleScore = double.Parse(simpleMatch.Groups[1].Value);
        var complexScore = double.Parse(complexMatch.Groups[1].Value);

        Assert.True(simpleScore > complexScore,
            $"Simple code ({simpleScore:F1}) should score higher than complex code ({complexScore:F1})");
    }
}

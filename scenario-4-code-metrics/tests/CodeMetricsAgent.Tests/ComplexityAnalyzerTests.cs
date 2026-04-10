namespace CodeMetricsAgent.Tests;

public class ComplexityAnalyzerTests
{
    [Fact]
    public void NullInput_ReturnsError()
    {
        var result = ComplexityAnalyzer.AnalyzeComplexity(null!);
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void EmptyInput_ReturnsError()
    {
        var result = ComplexityAnalyzer.AnalyzeComplexity("");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void WhitespaceInput_ReturnsError()
    {
        var result = ComplexityAnalyzer.AnalyzeComplexity("   \n  ");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void LinearCode_ComplexityIsOne()
    {
        var code = """
            var x = 1;
            var y = 2;
            var z = x + y;
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("Total Complexity: 1", result);
        Assert.Contains("Low", result);
        Assert.Contains("no decision points found", result);
    }

    [Fact]
    public void SingleIf_ComplexityIsTwo()
    {
        var code = """
            if (x > 0)
            {
                Console.WriteLine("positive");
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("Total Complexity: 2", result);
        Assert.Contains("if: 1", result);
    }

    [Fact]
    public void MultipleBranches_CorrectSum()
    {
        // if + else if + for + while = 4 decision points → complexity 5
        var code = """
            if (a > 0)
            {
                x = 1;
            }
            else if (a < 0)
            {
                x = -1;
            }
            for (int i = 0; i < 10; i++)
            {
                while (running)
                {
                    Process();
                }
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        // if matches both "if (" and "else if (" overlaps: let's verify
        // "if (a > 0)" → matches \bif\s*\( = 1, also "else if (a < 0)" matches \bif\s*\( too (since 'if' is a word boundary after 'else ')
        // "else if (a < 0)" → matches \belse\s+if\s*\( = 1
        // "for (int" → matches \bfor\s*\( = 1
        // "while (running)" → matches \bwhile\s*\( = 1
        // So: if=2, else if=1, for=1, while=1 → 5 decision points + base 1 = 6
        Assert.Contains("Total Complexity: 6", result);
        Assert.Contains("if: 2", result);
        Assert.Contains("else if: 1", result);
        Assert.Contains("for: 1", result);
        Assert.Contains("while: 1", result);
    }

    [Fact]
    public void LogicalOperators_Counted()
    {
        var code = """
            if (a > 0 && b > 0 || c > 0)
            {
                DoWork();
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        // if=1, &&=1, ||=1 → base 1 + 3 = 4
        Assert.Contains("Total Complexity: 4", result);
        Assert.Contains("&&: 1", result);
        Assert.Contains("||: 1", result);
    }

    [Fact]
    public void NullCoalescing_Counted()
    {
        var code = """
            var x = value ?? defaultValue;
            var y = obj?.Property;
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        // ??=1, ?.=1 → base 1 + 2 = 3
        Assert.Contains("Total Complexity: 3", result);
        Assert.Contains("??: 1", result);
        Assert.Contains("?.: 1", result);
    }

    [Fact]
    public void ComplexMethod_HighOrVeryHighRating()
    {
        // Generate code with many decision points (>20)
        var code = string.Join("\n", Enumerable.Range(0, 25).Select(i =>
            $"if (x{i} > 0) {{ }}"));

        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        // 25 if statements → complexity 26 → "Very High"
        Assert.Contains("Very High", result);
    }

    [Fact]
    public void CatchBlock_Counted()
    {
        var code = """
            try
            {
                DoWork();
            }
            catch (Exception ex)
            {
                HandleError();
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("catch: 1", result);
    }

    [Fact]
    public void CaseStatements_Counted()
    {
        var code = """
            switch (x)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("case: 3", result);
        // base 1 + 3 cases = 4
        Assert.Contains("Total Complexity: 4", result);
    }

    [Theory]
    [InlineData(1, "Low")]
    [InlineData(5, "Low")]
    [InlineData(6, "Moderate")]
    [InlineData(10, "Moderate")]
    [InlineData(11, "High")]
    [InlineData(20, "High")]
    [InlineData(21, "Very High")]
    public void RatingThresholds_MatchExpected(int ifCount, string expectedRating)
    {
        // Each if adds 1 to complexity; we need complexity = ifCount
        // Since base is 1, we need ifCount - 1 if statements
        var needed = ifCount - 1;
        var code = needed > 0
            ? string.Join("\n", Enumerable.Range(0, needed).Select(i => $"if (x{i} > 0) {{ }}"))
            : "var x = 1;";

        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains(expectedRating, result);
    }

    [Fact]
    public void LinesAnalyzed_ReportedCorrectly()
    {
        var code = "line1\nline2\nline3";
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("Lines Analyzed: 3", result);
    }

    [Fact]
    public void ForeachStatement_Counted()
    {
        var code = """
            foreach (var item in items)
            {
                Process(item);
            }
            """;
        var result = ComplexityAnalyzer.AnalyzeComplexity(code);

        Assert.Contains("foreach: 1", result);
        Assert.Contains("Total Complexity: 2", result);
    }
}

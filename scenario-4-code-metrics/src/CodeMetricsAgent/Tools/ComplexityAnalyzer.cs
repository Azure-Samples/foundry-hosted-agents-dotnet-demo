using System.ComponentModel;
using System.Text.RegularExpressions;

/// <summary>
/// Analyzes cyclomatic complexity of source code by counting decision points.
/// </summary>
static class ComplexityAnalyzer
{
    [Description("Analyzes cyclomatic complexity of source code by counting decision points (if, else if, while, for, foreach, case, &&, ||, catch, ??)")]
    public static string AnalyzeComplexity(
        [Description("Source code to analyze")] string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Error: No source code provided.";

        // Start with base complexity of 1
        int complexity = 1;

        // Decision point patterns (simplified — counts keywords outside strings/comments)
        var patterns = new Dictionary<string, string>
        {
            ["if"]      = @"\bif\s*\(",
            ["else if"] = @"\belse\s+if\s*\(",
            ["while"]   = @"\bwhile\s*\(",
            ["for"]     = @"\bfor\s*\(",
            ["foreach"] = @"\bforeach\s*\(",
            ["case"]    = @"\bcase\s+",
            ["&&"]      = @"&&",
            ["||"]      = @"\|\|",
            ["catch"]   = @"\bcatch\s*[\(\{]",
            ["??"]      = @"\?\?",
            ["?."]      = @"\?\."
        };

        var breakdown = new List<string>();
        foreach (var (name, pattern) in patterns)
        {
            var count = Regex.Matches(sourceCode, pattern).Count;
            if (count > 0)
            {
                complexity += count;
                breakdown.Add($"  {name}: {count}");
            }
        }

        var totalLines = sourceCode.Split('\n').Length;
        var rating = complexity switch
        {
            <= 5  => "Low — simple, easy to test",
            <= 10 => "Moderate — reasonable complexity",
            <= 20 => "High — consider refactoring",
            _     => "Very High — refactoring strongly recommended"
        };

        var result = $"""
            Cyclomatic Complexity Analysis:
              Total Complexity: {complexity}
              Rating: {rating}
              Lines Analyzed: {totalLines}
            
            Decision Points:
            """;

        if (breakdown.Count > 0)
            result += "\n" + string.Join("\n", breakdown);
        else
            result += "\n  (no decision points found — linear code)";

        return result;
    }
}

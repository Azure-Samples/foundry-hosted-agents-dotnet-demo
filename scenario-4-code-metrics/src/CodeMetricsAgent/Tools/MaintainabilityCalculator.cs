using System.ComponentModel;
using System.Text.RegularExpressions;

/// <summary>
/// Calculates a maintainability index (0–100) based on Halstead volume, cyclomatic complexity, and lines of code.
/// Uses a simplified adaptation of the Visual Studio maintainability index formula.
/// </summary>
static class MaintainabilityCalculator
{
    [Description("Calculates a maintainability index (0–100) for source code based on volume, complexity, and size. Higher is better.")]
    public static string CalculateMaintainability(
        [Description("Source code to analyze")] string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Error: No source code provided.";

        var lines = sourceCode.Split('\n');
        int loc = lines.Count(l => !string.IsNullOrWhiteSpace(l));

        // Simplified Halstead: count unique operators and operands
        var operators = new HashSet<string>();
        var operands = new HashSet<string>();
        int totalOperators = 0;
        int totalOperands = 0;

        var opPattern = new Regex(@"[+\-*/%=<>!&|^~?:]+|\.|\(|\)|\{|\}|\[|\]|;|,");
        var identPattern = new Regex(@"\b[a-zA-Z_]\w*\b");
        var numberPattern = new Regex(@"\b\d+(\.\d+)?\b");

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;

            foreach (Match m in opPattern.Matches(trimmed))
            {
                operators.Add(m.Value);
                totalOperators++;
            }
            foreach (Match m in identPattern.Matches(trimmed))
            {
                operands.Add(m.Value);
                totalOperands++;
            }
            foreach (Match m in numberPattern.Matches(trimmed))
            {
                operands.Add(m.Value);
                totalOperands++;
            }
        }

        // Halstead volume = N * log2(n) where N = total, n = unique
        int n = operators.Count + operands.Count;
        int totalN = totalOperators + totalOperands;
        double volume = n > 0 ? totalN * Math.Log2(n) : 0;

        // Cyclomatic complexity (simplified — same counting as ComplexityAnalyzer)
        int complexity = 1;
        var ccPatterns = new[] { @"\bif\s*\(", @"\bwhile\s*\(", @"\bfor\s*\(", @"\bforeach\s*\(",
                                  @"\bcase\s+", @"&&", @"\|\|", @"\bcatch\s*[\(\{]", @"\?\?" };
        foreach (var p in ccPatterns)
            complexity += Regex.Matches(sourceCode, p).Count;

        // Maintainability Index (Microsoft formula, normalized to 0–100):
        // MI = max(0, 171 - 5.2 * ln(V) - 0.23 * CC - 16.2 * ln(LOC)) * 100 / 171
        double mi = 171.0
            - 5.2 * (volume > 0 ? Math.Log(volume) : 0)
            - 0.23 * complexity
            - 16.2 * (loc > 0 ? Math.Log(loc) : 0);
        mi = Math.Max(0, mi) * 100.0 / 171.0;

        var rating = mi switch
        {
            >= 80 => "Highly Maintainable ✅",
            >= 50 => "Moderately Maintainable 🟡",
            >= 20 => "Difficult to Maintain ⚠️",
            _     => "Unmaintainable 🔴"
        };

        return $"""
            Maintainability Index: {mi:F1}/100
            Rating: {rating}

            Components:
              Halstead Volume: {volume:F1}
              Cyclomatic Complexity: {complexity}
              Lines of Code: {loc}

            Interpretation:
              80–100: Highly maintainable — clean, simple code
              50–79:  Moderate — some complexity, manageable
              20–49:  Difficult — consider refactoring
              0–19:   Unmaintainable — urgent refactoring needed
            """;
    }
}

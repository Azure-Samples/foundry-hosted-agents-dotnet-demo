using System.ComponentModel;

/// <summary>
/// Counts lines of code, blank lines, and comment lines in source code.
/// </summary>
static class LineCounter
{
    [Description("Counts total lines, code lines, blank lines, and comment lines in source code")]
    public static string CountLines(
        [Description("Source code to analyze")] string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Error: No source code provided.";

        var lines = sourceCode.Split('\n');
        int total = lines.Length;
        int blank = 0;
        int comment = 0;
        int code = 0;
        bool inBlockComment = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line))
            {
                blank++;
                continue;
            }

            if (inBlockComment)
            {
                comment++;
                if (line.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (line.StartsWith("//") || line.StartsWith("#"))
            {
                comment++;
                continue;
            }

            if (line.StartsWith("/*"))
            {
                comment++;
                if (!line.Contains("*/"))
                    inBlockComment = true;
                continue;
            }

            code++;
        }

        var codeRatio = total > 0 ? (double)code / total * 100 : 0;

        return $"""
            Line Count Analysis:
              Total Lines:   {total}
              Code Lines:    {code} ({codeRatio:F1}%)
              Comment Lines: {comment}
              Blank Lines:   {blank}
            """;
    }
}

namespace CodeMetricsAgent.Tests;

public class LineCounterTests
{
    [Fact]
    public void NullInput_ReturnsError()
    {
        var result = LineCounter.CountLines(null!);
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void EmptyInput_ReturnsError()
    {
        var result = LineCounter.CountLines("");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsError()
    {
        var result = LineCounter.CountLines("   \n   ");
        Assert.Contains("Error: No source code provided", result);
    }

    [Fact]
    public void MostlyBlankLines_CountedCorrectly()
    {
        // Need at least one non-whitespace char so IsNullOrWhiteSpace doesn't trigger
        var code = "x\n\n\n";
        var result = LineCounter.CountLines(code);

        // "x", "", "", "" → 1 code + 3 blank
        Assert.Contains("Blank Lines:   3", result);
        Assert.Contains("Code Lines:    1", result);
    }

    [Fact]
    public void SingleLineComments_Detected()
    {
        var code = "// this is a comment\n// another comment\nvar x = 1;";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Comment Lines: 2", result);
        Assert.Contains("Code Lines:    1", result);
    }

    [Fact]
    public void HashComments_Detected()
    {
        var code = "# Python comment\nx = 1";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Comment Lines: 1", result);
        Assert.Contains("Code Lines:    1", result);
    }

    [Fact]
    public void SingleLineBlockComment_Detected()
    {
        var code = "/* single line block comment */\nvar x = 1;";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Comment Lines: 1", result);
        Assert.Contains("Code Lines:    1", result);
    }

    [Fact]
    public void MultiLineBlockComment_AllLinesCounted()
    {
        var code = "/* start\n  middle\n  end */\nvar x = 1;";
        var result = LineCounter.CountLines(code);

        // Line 1: "/* start" → comment, sets inBlockComment=true
        // Line 2: "  middle" → comment (in block)
        // Line 3: "  end */" → comment (in block), sets inBlockComment=false
        // Line 4: "var x = 1;" → code
        Assert.Contains("Comment Lines: 3", result);
        Assert.Contains("Code Lines:    1", result);
    }

    [Fact]
    public void MixedCodeCommentsAndBlanks_CorrectBreakdown()
    {
        var code = "// header comment\n\nvar x = 1;\nvar y = 2;\n\n// footer";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Total Lines:   6", result);
        Assert.Contains("Code Lines:    2", result);
        Assert.Contains("Comment Lines: 2", result);
        Assert.Contains("Blank Lines:   2", result);
    }

    [Fact]
    public void CodeRatio_CalculatedCorrectly()
    {
        // 2 code lines out of 4 total = 50.0%
        var code = "var x = 1;\n\n// comment\nvar y = 2;";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Code Lines:    2 (50.0%)", result);
    }

    [Fact]
    public void AllCodeLines_RatioIs100Percent()
    {
        var code = "var x = 1;\nvar y = 2;\nvar z = 3;";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Code Lines:    3 (100.0%)", result);
    }

    [Fact]
    public void TotalLineCount_Correct()
    {
        var code = "a\nb\nc\nd\ne";
        var result = LineCounter.CountLines(code);

        Assert.Contains("Total Lines:   5", result);
    }

    [Fact]
    public void BlockCommentNotClosed_RemainingLinesAreComments()
    {
        // Block comment opened but never closed
        var code = "var a = 1;\n/* open block\nstill comment\nstill comment";
        var result = LineCounter.CountLines(code);

        // Line 1: code
        // Line 2: "/* open block" → comment, inBlockComment=true
        // Line 3: "still comment" → comment (in block)
        // Line 4: "still comment" → comment (in block)
        Assert.Contains("Code Lines:    1", result);
        Assert.Contains("Comment Lines: 3", result);
    }
}

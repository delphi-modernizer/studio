using System;
using System.IO;

namespace DelphiModernizer.Studio.Application.Abstractions.Workspaces;

/// <summary>Reads the words of one row of a register table, for pages that show a record by what it says.</summary>
public static class RegisterRows
{
    /// <param name="relativePath">The register, relative to the workspace, as the trace check reports it.</param>
    /// <param name="line">The row's one-based line.</param>
    /// <param name="column">The zero-based cell after the ID; 0 is the cell right after it.</param>
    /// <returns>The cell's text, or null when the line is not a table row or has no such cell.</returns>
    public static string? Cell(string workspaceRoot, string relativePath, int line, int column)
    {
        var path = Path.Combine(workspaceRoot, relativePath);

        if (line < 1 || !File.Exists(path))
        {
            return null;
        }

        var lines = File.ReadAllLines(path);
        var cells = line <= lines.Length ? lines[line - 1].Split('|', StringSplitOptions.TrimEntries) : [];

        return cells.Length > column + 2 ? cells[column + 2] : null;
    }
}

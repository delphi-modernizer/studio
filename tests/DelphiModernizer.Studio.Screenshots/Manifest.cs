using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>manifest.json: each capture, the canvas board it pairs with and that artboard's label.</summary>
/// <remarks>The side-by-side page reads it; renaming a field breaks that page.</remarks>
internal sealed class Manifest
{
    private readonly List<JsonObject> _captures = [];

    public void Add(string file, string page, string state, string theme, int width)
    {
        var (board, label) = Boards.For(page, state, theme, width);
        _captures.Add(new JsonObject {
            ["file"] = file,
            ["page"] = page,
            ["state"] = state,
            ["theme"] = theme,
            ["board"] = board,
            ["label"] = label,
        });
    }

    public void Write(string path)
    {
        var captures = new JsonArray([.. _captures]);
        File.WriteAllText(path, captures.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}

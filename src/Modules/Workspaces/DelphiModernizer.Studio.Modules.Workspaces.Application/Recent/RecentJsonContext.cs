using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true, NewLine = "\n")]
[JsonSerializable(typeof(List<RecentWorkspace>))]
internal sealed partial class RecentJsonContext : JsonSerializerContext;

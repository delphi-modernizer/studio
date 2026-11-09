using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Tests;

/// <summary>A small Delphi tree and a workspace created from it in a temporary folder, opened in a session.</summary>
internal sealed class TestWorkspace : IDisposable
{
    private const int _deleteAttempts = 10;
    private static readonly TimeSpan _deleteRetryDelay = TimeSpan.FromMilliseconds(100);

    private TestWorkspace(string parent, Workspace workspace)
    {
        Parent = parent;
        Workspace = workspace;
        Session.Open(workspace);
    }

    public string Parent { get; }

    public Workspace Workspace { get; }

    public WorkspaceSession Session { get; } = new();

    public static string CreateLegacyTree(string parent)
    {
        string legacy = Path.Combine(parent, "AcmeERP");
        Directory.CreateDirectory(Path.Combine(legacy, "Sales"));
        File.WriteAllText(Path.Combine(legacy, "ErpApp.dpr"), "program ErpApp;\n\nuses\n  Pricing in 'Sales\\Pricing.pas',\n  Utils in 'Utils.pas';\n\nbegin\nend.\n");
        File.WriteAllText(Path.Combine(legacy, "Sales", "Pricing.pas"), "unit Pricing;\n\ninterface\n\nfunction Discount(const ALimit: Currency): Currency;\n\nimplementation\n\nuses\n  Utils;\n\nfunction Discount(const ALimit: Currency): Currency;\nbegin\n  if ALimit >= 10000 then\n    Result := ALimit * 0.075\n  else\n    Result := ALimit * 0.025;\nend;\n\nend.\n");
        File.WriteAllText(Path.Combine(legacy, "Utils.pas"), "unit Utils;\n\ninterface\n\nprocedure LogValue(const AName: string);\n\nimplementation\n\nuses\n  Pricing;\n\nprocedure LogValue(const AName: string);\nbegin\nend;\n\nend.\n");

        return legacy;
    }

    public static async Task<TestWorkspace> CreateAsync()
    {
        string parent = Directory.CreateTempSubdirectory("dmz-studio-").FullName;
        var request = new InitRequest(
            Path.Combine(parent, "ws"),
            "Acme ERP",
            CreateLegacyTree(parent),
            "dotnet",
            1252,
            "Test Person <test@example.com>",
            null,
            InitializeGit: false);
        var created = await WorkspaceInitializer.InitializeAsync(request, null, CancellationToken.None);

        return new TestWorkspace(parent, created.Value);
    }

    /// <remarks>
    /// A run ends only once its CLI has exited, but on Windows a file it read can stay held for a moment after that,
    /// by something outside the test such as a scanner; the delete tries again for up to a second before failing.
    /// </remarks>
    public void Dispose()
    {
        for (int attempt = 1; attempt < _deleteAttempts; attempt++)
        {
            try
            {
                Directory.Delete(Parent, recursive: true);

                return;
            }
            catch (IOException)
            {
                Thread.Sleep(_deleteRetryDelay);
            }
        }

        Directory.Delete(Parent, recursive: true);
    }
}

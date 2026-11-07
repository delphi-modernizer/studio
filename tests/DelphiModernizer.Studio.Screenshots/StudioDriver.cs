using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FlaUIApplication = FlaUI.Core.Application;
using FlaUICapture = FlaUI.Core.Capturing.Capture;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>One running app: finds its parts by their automation names and captures the window at each size.</summary>
internal sealed class StudioDriver : IDisposable
{
    private static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan _findTimeout = TimeSpan.FromSeconds(20);
    private static readonly Point _origin = new(16, 16);
    private const string _expanderClass = "Microsoft.UI.Xaml.Controls.Expander";

    private readonly UIA3Automation _automation = new();
    private readonly FlaUIApplication _app;
    private readonly Window _window;
    private readonly string _output;
    private readonly string _theme;
    private readonly IReadOnlyList<Size> _sizes;

    public StudioDriver(string exe, string output, string theme, IReadOnlyList<Size> sizes)
    {
        _output = output;
        _theme = theme;
        _sizes = sizes;
        _app = FlaUIApplication.Launch(new ProcessStartInfo(exe) {
            WorkingDirectory = Path.GetDirectoryName(exe),
        });
        _window = _app.GetMainWindow(_automation, _startTimeout)
            ?? throw new InvalidOperationException($"No window within {_startTimeout.TotalSeconds} s.");
        _window.SetForeground();
        Settle(5000);
    }

    public bool HasExited => _app.HasExited;

    /// <summary>Captures the page at each size, named as docs/design/ui-screenshots.md says.</summary>
    public void Capture(string page, string state)
    {
        foreach (var size in _sizes)
        {
            EnsureRunning();
            var frame = Desktop.SetVisibleFrame(_window.Properties.NativeWindowHandle.Value, _origin, size);

            if (frame.Size != size)
            {
                throw new InvalidOperationException(
                    $"The window is {frame.Width} x {frame.Height}, not {size.Width} x {size.Height}.");
            }

            _window.SetForeground();
            Mouse.MoveTo(new Point(frame.Right + 40, frame.Bottom + 40));
            Settle(1200);

            var file = Path.Combine(_output, $"{page}-{state}-{size.Width}x{size.Height}-{_theme}.png");
            FlaUICapture
                .Rectangle(frame)
                .ToFile(file);
            Console.WriteLine($"  {Path.GetFileName(file)}");
        }
    }

    /// <summary>Selects a pane item by its automation ID, the page key such as <c>SignOffs</c>.</summary>
    public void Navigate(string pageKey)
    {
        var item = Find(cf => cf.ByAutomationId(pageKey), pageKey);

        if (item.Patterns.SelectionItem.TryGetPattern(out var selection))
        {
            selection.Select();
        }
        else
        {
            item.Patterns.Invoke.Pattern.Invoke();
        }

        Settle(2500);
    }

    public void Invoke(string name)
    {
        Find(cf => cf.ByName(name), name).Patterns.Invoke.Pattern.Invoke();
        Settle(2500);
    }

    /// <summary>Clicks the first row of a list; UI Automation cannot raise ItemClick.</summary>
    public void ClickFirstItem(string listName)
    {
        var list = Find(
            cf => cf
                .ByName(listName)
                .And(cf.ByControlType(ControlType.List)),
            listName);
        var row = Retry
            .WhileNull(() => list.FindFirstChild(cf => cf.ByControlType(ControlType.ListItem)), _findTimeout)
            .Result ?? throw new InvalidOperationException($"{listName} has no rows.");
        row.Click();
        Settle(4000);
    }

    /// <summary>Selects each item of a SelectorBar in turn, calling back with the item's name.</summary>
    public void ForEachSection(string barName, Action<string> capture)
    {
        var bar = Find(cf => cf.ByName(barName), barName);
        var items = bar
            .FindAllDescendants()
            .Where(e => e.Patterns.SelectionItem.IsSupported)
            .ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException($"{barName} has no sections.");
        }

        foreach (var item in items)
        {
            var name = item.Name;
            item.Patterns.SelectionItem.Pattern.Select();
            Settle(1500);
            capture(name);
        }
    }

    /// <summary>Expands the first expander when none is open, so a page shows one expanded.</summary>
    public void EnsureOneExpanded()
    {
        var expanders = Retry
            .WhileEmpty(() => _window.FindAllDescendants(cf => cf.ByClassName(_expanderClass)), _findTimeout)
            .Result;

        if (expanders is null || expanders.Length == 0)
        {
            throw new InvalidOperationException("The page has no expanders.");
        }

        var patterns = expanders
            .Select(e => e.Patterns.ExpandCollapse.PatternOrDefault)
            .OfType<IExpandCollapsePattern>()
            .ToList();

        if (patterns.TrueForAll(p => p.ExpandCollapseState.Value is not ExpandCollapseState.Expanded))
        {
            patterns[0].Expand();
            Settle(1500);
        }
    }

    public static string Slug(string name)
    {
        var slug = new StringBuilder();

        foreach (var c in name.ToLowerInvariant())
        {
            slug.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        var text = slug.ToString();

        while (text.Contains("--", StringComparison.Ordinal))
        {
            text = text.Replace("--", "-", StringComparison.Ordinal);
        }

        return text.Trim('-');
    }

    public void Dispose()
    {
        if (!_app.HasExited)
        {
            _app.Close();
            Retry.WhileFalse(() => _app.HasExited, TimeSpan.FromSeconds(10), throwOnTimeout: false);
        }

        if (!_app.HasExited)
        {
            _app.Kill();
        }

        _app.Dispose();
        _automation.Dispose();
    }

    private AutomationElement Find(Func<ConditionFactory, ConditionBase> condition, string what)
    {
        EnsureRunning();

        return Retry
            .WhileNull(() => _window.FindFirstDescendant(condition), _findTimeout)
            .Result ?? throw new InvalidOperationException($"No {what} within {_findTimeout.TotalSeconds} s.");
    }

    private void EnsureRunning()
    {
        if (_app.HasExited)
        {
            throw new InvalidOperationException($"The app exited with code {_app.ExitCode}.");
        }
    }

    /// <remarks>Pages load from the engine off the UI thread; a fixed wait lets the load and layout finish.</remarks>
    private static void Settle(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }
}

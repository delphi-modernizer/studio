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
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaUIApplication = FlaUI.Core.Application;
using FlaUICapture = FlaUI.Core.Capturing.Capture;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>One running app: finds its parts by their automation names and captures the window at each size.</summary>
internal sealed class StudioDriver : IDisposable
{
    public static readonly Size Wide = new(1280, 800);
    public static readonly Size Minimum = new(960, 576);
    public static readonly IReadOnlyList<Size> Sizes = [Wide, Minimum];

    private static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan _findTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _poll = TimeSpan.FromMilliseconds(20);

    /// <remarks>Away from the desktop's icons, which show through the window's one-pixel border.</remarks>
    private static readonly Point _origin = new(480, 120);

    private const string _expanderClass = "Microsoft.UI.Xaml.Controls.Expander";

    /// <summary>The title bar's height; the pane's artboard starts below it.</summary>
    private const int _titleBarHeight = 48;
    private const int _paneWidth = 240;

    private readonly UIA3Automation _automation = new();
    private readonly FlaUIApplication _app;
    private readonly Window _window;
    private readonly string _output;
    private readonly string _theme;
    private readonly Manifest _manifest;

    public StudioDriver(
        string exe,
        string output,
        string theme,
        Manifest manifest,
        IReadOnlyDictionary<string, string?> environment)
    {
        _output = output;
        _theme = theme;
        _manifest = manifest;
        var start = new ProcessStartInfo(exe) {
            WorkingDirectory = Path.GetDirectoryName(exe),
            UseShellExecute = false,
        };

        foreach (var (name, value) in environment)
        {
            start.Environment[name] = value;
        }

        _app = FlaUIApplication.Launch(start);
        _window = _app.GetMainWindow(_automation, _startTimeout)
            ?? throw new InvalidOperationException($"No window within {_startTimeout.TotalSeconds} s.");
        _window.SetForeground();
        Settle(5000);
    }

    public bool HasExited => _app.HasExited;

    private nint Handle => _window.Properties.NativeWindowHandle.Value;

    /// <summary>Captures the window at each size, named as docs/design/ui-screenshots.md says.</summary>
    /// <param name="arrange">Runs after each resize, before the capture: a hover, a focus, a trigger.</param>
    public void Capture(string page, string state, Action? arrange = null, IReadOnlyList<Size>? sizes = null)
    {
        foreach (var size in sizes ?? Sizes)
        {
            var frame = Resize(size);
            arrange?.Invoke();
            Save(page, state, size, frame);
        }
    }

    /// <summary>Captures one part of the window at 1280 × 800: the title bar or the navigation pane.</summary>
    public void CapturePart(string page, string state, bool pane)
    {
        var frame = Resize(Wide);
        var part = pane
            ? new Rectangle(frame.Left, frame.Top + _titleBarHeight, _paneWidth, frame.Height - _titleBarHeight)
            : new Rectangle(frame.Left, frame.Top, frame.Width, _titleBarHeight);
        Save(page, state, part.Size, part);
    }

    /// <summary>Sizes the window and moves the pointer off it, so nothing shows hovered.</summary>
    public Rectangle Resize(Size size)
    {
        EnsureRunning();
        var frame = Desktop.SetVisibleFrame(Handle, _origin, size);

        if (frame.Size != size)
        {
            throw new InvalidOperationException(
                $"The window is {frame.Width} x {frame.Height}, not {size.Width} x {size.Height}.");
        }

        _window.SetForeground();
        Mouse.MoveTo(new Point(frame.Right + 40, frame.Bottom + 40));
        Settle(1200);

        return frame;
    }

    /// <summary>Selects a pane item by its automation ID, the page key such as <c>SignOffs</c>.</summary>
    public void Navigate(string pageKey, int settle = 2500)
    {
        var item = Find(e => Id(e) == pageKey, pageKey);

        if (item.Patterns.SelectionItem.TryGetPattern(out var selection))
        {
            selection.Select();
        }
        else
        {
            item.Patterns.Invoke.Pattern.Invoke();
        }

        Settle(settle);
    }

    /// <summary>Invokes the button with this automation ID, such as the title bar's PaneToggleButton.</summary>
    public void InvokeById(string automationId)
    {
        Find(e => Id(e) == automationId, automationId).Patterns.Invoke.Pattern.Invoke();
        Settle(2500);
    }

    /// <summary>Invokes the button or link with this name, or clicks the text of a button that has no name.</summary>
    public void Invoke(string name, int settle = 2500)
    {
        EnsureRunning();
        var element = Retry
            .WhileNull(
                () => Descendants().FirstOrDefault(e => IsAction(e) && Matches(e, name))
                    ?? Descendants().FirstOrDefault(e => Type(e) == ControlType.Text && NameOf(e) == name),
                _findTimeout,
                TimeSpan.FromMilliseconds(500))
            .Result ?? throw new InvalidOperationException($"No {name} within {_findTimeout.TotalSeconds} s.");

        if (Type(element) == ControlType.Text)
        {
            Mouse.Click(element.BoundingRectangle.Center());
        }
        else if (element.Properties.IsEnabled.ValueOrDefault)
        {
            element.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            throw new InvalidOperationException($"{name} is disabled.");
        }

        Settle(settle);
    }

    /// <summary>Clicks the button showing <paramref name="name"/> level with the text <paramref name="row"/>.</summary>
    /// <remarks>
    /// UI Automation exposes neither the row's panel nor a name for a button with a glyph and text, so the button's
    /// own text is found level with the row's title and clicked, as a person clicks it.
    /// </remarks>
    public void ClickBeside(string row, string name)
    {
        var title = Find(e => Type(e) == ControlType.Text && NameOf(e) == row, row).BoundingRectangle;
        var label = Descendants()
            .Where(e => Type(e) is ControlType.Text or ControlType.Button && NameOf(e) == name)
            .FirstOrDefault(e => e.BoundingRectangle.Top <= title.Bottom + 40
                && e.BoundingRectangle.Bottom >= title.Top)
            ?? throw new InvalidOperationException($"No {name} beside {row}.");
        Mouse.Click(label.BoundingRectangle.Center());
        Settle(2500);
    }

    /// <summary>Clicks the list row whose name contains <paramref name="text"/>; UIA cannot raise ItemClick.</summary>
    public void ClickItem(string listName, string text)
    {
        var list = Find(e => Type(e) == ControlType.List && NameOf(e) == listName, listName);
        var row = Retry
            .WhileNull(
                () => list
                    .FindAllChildren(c => c.ByControlType(ControlType.ListItem))
                    .FirstOrDefault(r => NameOf(r).Contains(text, StringComparison.Ordinal)),
                _findTimeout)
            .Result ?? throw new InvalidOperationException($"{listName} has no row with {text}.");
        row.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
        row.Click();
        Settle(4000);
    }

    /// <summary>Selects the list, radio or SelectorBar item whose name starts with <paramref name="name"/>.</summary>
    public void Select(string name)
    {
        var item = Find(e => e.Patterns.SelectionItem.IsSupported && Starts(e, name), name);
        item.Patterns.SelectionItem.Pattern.Select();
        Settle(2000);
    }

    /// <summary>Selects each item of a SelectorBar in turn, calling back with the item's name.</summary>
    public void ForEachSection(string barName, Action<string> capture)
    {
        var bar = Find(e => NameOf(e) == barName, barName);
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
            var name = NameOf(item);
            item.Patterns.SelectionItem.Pattern.Select();
            Settle(1500);
            capture(name);
        }
    }

    public void Toggle(string name)
    {
        Find(e => e.Patterns.Toggle.IsSupported && Matches(e, name), name).Patterns.Toggle.Pattern.Toggle();
        Settle(2500);
    }

    /// <summary>Types into the text box with this name, then moves focus on as a person tabbing out would.</summary>
    public void Type(string name, string text, bool tabOut = false)
    {
        var box = Find(e => Type(e) == ControlType.Edit && Matches(e, name), name);
        box.Focus();
        box.Patterns.Value.Pattern.SetValue(text);

        if (tabOut)
        {
            Keyboard.Type(VirtualKeyShort.TAB);
        }

        Settle(800);
    }

    /// <summary>Expands the expander whose name starts with <paramref name="name"/>, or the first one.</summary>
    public void Expand(string? name = null)
    {
        var expanders = Retry
            .WhileEmpty(() => _window.FindAllDescendants(cf => cf.ByClassName(_expanderClass)), _findTimeout)
            .Result ?? [];
        var expander = expanders.FirstOrDefault(e => name is null || Matches(e, name))
            ?? throw new InvalidOperationException($"No expander {name}.");

        if (expander.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value is not ExpandCollapseState.Expanded)
        {
            expander.Patterns.ExpandCollapse.Pattern.Expand();
            Settle(1500);
        }
    }

    public void Hover(string automationId)
    {
        var element = Find(e => Id(e) == automationId, automationId);
        element.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
        Mouse.MoveTo(element.BoundingRectangle.Center());
        Settle(2500);
    }

    /// <summary>Gives keyboard focus as Tab would, so the focus ring shows.</summary>
    public void FocusWithKeyboard(string automationId)
    {
        var element = Find(e => Id(e) == automationId, automationId);
        element.Focus();
        Keyboard.Type(VirtualKeyShort.TAB);
        Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.TAB);
        Settle(1500);
    }

    public void ScrollIntoView(string automationId)
    {
        Find(e => Id(e) == automationId, automationId).Patterns.ScrollItem.Pattern.ScrollIntoView();
        Settle(1500);
    }

    /// <summary>Waits until an element with this name, or whose name starts with it, shows.</summary>
    public void WaitFor(string name, TimeSpan timeout)
    {
        EnsureRunning();
        var found = Retry
            .WhileNull(
                () => _window.FindFirstDescendant(c => c.ByName(name))
                    ?? Descendants().FirstOrDefault(e => Starts(e, name)),
                timeout,
                TimeSpan.FromMilliseconds(500))
            .Result;

        if (found is null)
        {
            throw new TimeoutException($"{name} did not show within {timeout.TotalSeconds} s.");
        }
    }

    /// <summary>Waits for a state that lasts a second or so, with one native query every few milliseconds.</summary>
    /// <remarks>A walk of the whole tree takes longer than a short scan, so a brief state is matched exactly.</remarks>
    public void WaitBriefly(Func<ConditionFactory, ConditionBase> condition, string what, TimeSpan timeout)
    {
        EnsureRunning();
        var found = Retry
            .WhileNull(() => _window.FindFirstDescendant(condition), timeout, _poll)
            .Result;

        if (found is null)
        {
            throw new TimeoutException($"{what} did not show within {timeout.TotalSeconds} s.");
        }
    }

    /// <summary>Waits until the button with this name can be pressed, as a person waits before pressing it.</summary>
    public void WaitEnabled(string name, TimeSpan timeout)
    {
        EnsureRunning();
        var enabled = Retry
            .WhileFalse(
                () => Descendants().Any(e => IsAction(e) && Matches(e, name) && e.Properties.IsEnabled.ValueOrDefault),
                timeout,
                TimeSpan.FromMilliseconds(500))
            .Result;

        if (!enabled)
        {
            throw new TimeoutException($"{name} is still disabled after {timeout.TotalSeconds} s.");
        }
    }

    /// <summary>Saves the window as it is when a step fails, beside the captures but outside the manifest.</summary>
    public void SaveFailure(string step)
    {
        var folder = Path.Combine(_output, "failures");
        Directory.CreateDirectory(folder);
        FlaUICapture
            .Rectangle(Desktop.VisibleFrame(Handle))
            .ToFile(Path.Combine(folder, $"{_theme}-{Slug(step)}.png"));
    }

    /// <summary>Waits until no element whose name starts with <paramref name="name"/> shows.</summary>
    public void WaitGone(string name, TimeSpan timeout)
    {
        EnsureRunning();

        if (!Retry.WhileTrue(() => Descendants().Any(e => Starts(e, name)), timeout, throwOnTimeout: false).Result)
        {
            return;
        }

        throw new TimeoutException($"{name} still shows after {timeout.TotalSeconds} s.");
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

        // A section's name ends with its count, as "Code modules, 7"; the file name must not change with the data.
        return text
            .TrimEnd("0123456789".ToCharArray())
            .Trim('-');
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

    private static bool IsAction(AutomationElement element)
    {
        return Type(element) is ControlType.Button or ControlType.Hyperlink or ControlType.SplitButton
            && element.Patterns.Invoke.IsSupported;
    }

    /// <summary>A name equal to the text, or starting with it and a comma, as names with a count do.</summary>
    private static bool Matches(AutomationElement element, string name)
    {
        var actual = NameOf(element);

        return actual == name || actual.StartsWith(name + ",", StringComparison.Ordinal);
    }

    private static bool Starts(AutomationElement element, string text)
    {
        return NameOf(element).StartsWith(text, StringComparison.Ordinal);
    }

    /// <remarks>A plain property getter throws where the element lacks the property; these read it as empty.</remarks>
    private static string NameOf(AutomationElement element)
    {
        return element.Properties.Name.ValueOrDefault ?? string.Empty;
    }

    private static string Id(AutomationElement element)
    {
        return element.Properties.AutomationId.ValueOrDefault ?? string.Empty;
    }

    private static ControlType Type(AutomationElement element)
    {
        return element.Properties.ControlType.ValueOrDefault;
    }

    private void Save(string page, string state, Size size, Rectangle area)
    {
        var file = Path.Combine(_output, $"{page}-{state}-{size.Width}x{size.Height}-{_theme}.png");
        FlaUICapture
            .Rectangle(area)
            .ToFile(file);
        _manifest.Add(Path.GetFileName(file), page, state, _theme, size.Width);
        Console.WriteLine($"  {Path.GetFileName(file)}");
    }

    /// <remarks>Dialogs and flyouts can be popups outside the window; the app's other windows are searched.</remarks>
    private IEnumerable<AutomationElement> Descendants()
    {
        return _window
            .FindAllDescendants()
            .Concat(_automation
                .GetDesktop()
                .FindAllChildren(c => c.ByProcessId(_app.ProcessId))
                .Where(w => w.Properties.NativeWindowHandle.ValueOrDefault != Handle)
                .SelectMany(w => w.FindAllDescendants()));
    }

    private AutomationElement Find(Func<AutomationElement, bool> predicate, string what)
    {
        EnsureRunning();

        return Retry
            .WhileNull(() => Descendants().FirstOrDefault(predicate), _findTimeout, TimeSpan.FromMilliseconds(500))
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

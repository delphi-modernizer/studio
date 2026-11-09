using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using CaptureImage = FlaUI.Core.Capturing.CaptureImage;
using FlaUIApplication = FlaUI.Core.Application;
using FlaUICapture = FlaUI.Core.Capturing.Capture;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>One running app: finds its parts by their automation names and captures the window at each size.</summary>
internal sealed class StudioDriver : IDisposable
{
    /// <summary>A progress bar seen partway: its value and where it is on the screen.</summary>
    private sealed record Bar(IRangeValuePattern Range, Rectangle Bounds);

    public static readonly Size Wide = new(1280, 800);
    public static readonly Size Minimum = new(960, 576);
    public static readonly IReadOnlyList<Size> Sizes = [Wide, Minimum];

    private static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan _findTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _poll = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan _stillShowsTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan _barWatch = TimeSpan.FromSeconds(3);

    /// <summary>How far apart, summed over red, green and blue, a bar's fill, track and card are drawn.</summary>
    private const int _drawnApart = 60;

    /// <remarks>Away from the desktop's icons, which show through the window's one-pixel border.</remarks>
    private static readonly Point _origin = new(480, 120);

    private const string _expanderClass = "Microsoft.UI.Xaml.Controls.Expander";

    /// <summary>The title bar's height; the pane's artboard starts below it.</summary>
    private const int _titleBarHeight = 48;
    private const int _paneWidth = 240;

    /// <summary>How long WinUI's scroll indicator stays after a page scrolls, resizes or meets the pointer.</summary>
    private const int _indicatorFade = 3000;

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
            Save(page, state, size, FlaUICapture.Rectangle(frame));
        }
    }

    /// <summary>Captures a state that shows only while the app works, with the app held to one processor.</summary>
    /// <remarks>
    /// The large tree's scan takes about 0.6 s on a runner's four processors, less than finding its first frame can take.
    /// The screen is kept only if the state still shows once it is taken; a progress bar counts only partway, before
    /// and after, since the canvas draws one partly filled. On one processor the app draws its frames late, so another
    /// state's screen waits a quarter second after it shows. A state can end before it is taken, so it is tried up to
    /// five times.
    /// </remarks>
    /// <param name="start">Starts the work.</param>
    /// <param name="shows">The element that shows only while the app works.</param>
    public void CaptureWhileBusy(
        string page,
        string state,
        Action start,
        Func<ConditionFactory, ConditionBase> shows,
        IReadOnlyList<Size>? sizes = null)
    {
        var process = Process.GetProcessById(_app.ProcessId);
        nint every = process.ProcessorAffinity;

        foreach (var size in sizes ?? Sizes)
        {
            var frame = Resize(size);
            process.ProcessorAffinity = 1;

            try
            {
                var shot = TryTimes(5, () => {
                    start();

                    var bars = Retry.WhileNull(() => Busy(shows), _findTimeout, _poll).Result
                        ?? throw new TimeoutException($"{page} {state} did not show in {_findTimeout.TotalSeconds} s.");
                    var image = bars.Count > 0 ? DrawnWhilePartway(frame, bars) : ShotWhileShows(frame, shows);

                    return image ?? throw new TimeoutException($"{page} {state} ended as it was taken.");
                });
                Save(page, state, size, shot);
            }
            finally
            {
                process.ProcessorAffinity = every;
            }
        }
    }

    /// <remarks>A state that lasts a second or two can still end between two UI Automation queries.</remarks>
    private static T TryTimes<T>(int times, Func<T> action)
    {
        for (int attempt = 1; attempt < times; attempt++)
        {
            try
            {
                return action();
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"  missed, trying again ({attempt} of {times})");
            }
        }

        return action();
    }

    /// <summary>What shows of a state that lasts only while the app works.</summary>
    /// <returns>
    /// Null while the state does not show; the bars that show partway, or none when the state is not a bar.
    /// </returns>
    private List<Bar>? Busy(Func<ConditionFactory, ConditionBase> shows)
    {
        try
        {
            var found = _window.FindAllDescendants(shows);

            if (found.Any(e => Type(e) != ControlType.ProgressBar))
            {
                return [];
            }

            // A bar keeps the last work's value while it is indeterminate again, so it counts only partway, as the
            // canvas draws it.
            var bars = found
                .Select(e => e.Patterns.RangeValue.PatternOrDefault is { } range
                    ? new Bar(range, e.BoundingRectangle)
                    : null)
                .OfType<Bar>()
                .ToList();

            return bars.Count > 0 && GoesPartway(bars) ? bars : null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>Whether any of the bars found shows partway within a few seconds.</summary>
    /// <remarks>
    /// Their values are read on their own, all together: with the app on one processor, a walk of the window can take
    /// longer than a bar's partway does once the files are cached, and a spinner beside a bar may offer a value that
    /// never moves.
    /// </remarks>
    private static bool GoesPartway(IReadOnlyList<Bar> bars)
    {
        return Retry
            .WhileFalse(() => bars.Any(b => IsPartway(b.Range)), _barWatch, _poll)
            .Result;
    }

    /// <summary>The screen once the state has had a quarter second to draw, or null if the state has ended.</summary>
    private CaptureImage? ShotWhileShows(Rectangle frame, Func<ConditionFactory, ConditionBase> shows)
    {
        Settle(250);
        var image = FlaUICapture.Rectangle(frame);

        if (StillShows(shows))
        {
            return image;
        }

        image.Dispose();

        return null;
    }

    /// <summary>The first screen that draws a bar partway while its value still is, or null once none is.</summary>
    /// <remarks>
    /// A scan of cached files stays partway for only a few frames, which a fixed wait can miss, so the screen is taken
    /// again and again until one draws a bar partway.
    /// </remarks>
    private static CaptureImage? DrawnWhilePartway(Rectangle frame, IReadOnlyList<Bar> bars)
    {
        var watch = Stopwatch.StartNew();

        while (watch.Elapsed < _barWatch)
        {
            var image = FlaUICapture.Rectangle(frame);
            var partway = bars
                .Where(b => IsPartway(b.Range))
                .ToList();

            if (partway.Any(b => DrawnPartway(image, frame, b.Bounds)))
            {
                return image;
            }

            image.Dispose();

            if (partway.Count == 0)
            {
                return null;
            }

            Thread.Sleep(_poll);
        }

        return null;
    }

    /// <summary>Whether the screen shows the bar partly filled: its fill at one end, its track at the other.</summary>
    /// <remarks>
    /// On one processor the app can report a value some time before it draws it, so the screen itself is read: a bar
    /// not drawn yet shows its track at both ends, a full one its fill, and one still moving as indeterminate a short
    /// fill with no track, the one-pixel line on the bar's middle row.
    /// </remarks>
    private static bool DrawnPartway(CaptureImage image, Rectangle frame, Rectangle bar)
    {
        int y = bar.Top + (bar.Height / 2) - frame.Top;
        int start = bar.Left + 2 - frame.Left;
        int end = bar.Right - 3 - frame.Left;

        if (y < 3 || y >= image.Bitmap.Height || start < 0 || end >= image.Bitmap.Width || start >= end)
        {
            return false;
        }

        var fill = image.Bitmap.GetPixel(start, y);
        var track = image.Bitmap.GetPixel(end, y);
        var behind = image.Bitmap.GetPixel(end, y - 3);

        return Apart(fill, track) && Apart(track, behind);
    }

    private static bool Apart(Color one, Color other)
    {
        return Math.Abs(one.R - other.R) + Math.Abs(one.G - other.G) + Math.Abs(one.B - other.B) > _drawnApart;
    }

    private static bool IsPartway(IRangeValuePattern bar)
    {
        try
        {
            double value = bar.Value.ValueOrDefault;

            return value > 0 && value < bar.Maximum.ValueOrDefault;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <summary>Captures one part of the window at 1280 × 800: the title bar or the navigation pane.</summary>
    public void CapturePart(string page, string state, bool pane)
    {
        var frame = Resize(Wide);
        var part = pane
            ? new Rectangle(frame.Left, frame.Top + _titleBarHeight, _paneWidth, frame.Height - _titleBarHeight)
            : new Rectangle(frame.Left, frame.Top, frame.Width, _titleBarHeight);
        Save(page, state, part.Size, FlaUICapture.Rectangle(part));
    }

    /// <summary>Sizes the window and moves the pointer off it, so nothing shows hovered.</summary>
    /// <remarks>
    /// A page shows its scroll bar for a moment after its height changes, as a resize does, so the wait outlasts it.
    /// </remarks>
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
        MovePointerOff();
        Settle(_indicatorFade);

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

    /// <summary>Invokes the button or link with this name, or clicks the text of a button that has no name.</summary>
    public void Invoke(string name, int settle = 2500)
    {
        EnsureRunning();
        var element = Retry
            .WhileNull(
                () => Descendants().FirstOrDefault(e => IsAction(e) && Matches(e, name))
                    ?? Descendants().FirstOrDefault(e => Type(e) == ControlType.Text && NameOf(e) == name),
                _findTimeout,
                TimeSpan.FromMilliseconds(500),
                ignoreException: true)
            .Result ?? throw new InvalidOperationException($"No {name} within {_findTimeout.TotalSeconds} s.");

        if (Type(element) == ControlType.Text)
        {
            element = ButtonAround(element);
        }

        if (element.Properties.IsEnabled.ValueOrDefault)
        {
            element.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            throw new InvalidOperationException($"{name} is disabled.");
        }

        Settle(settle);
    }

    /// <summary>Presses the button showing <paramref name="name"/> beside the text <paramref name="row"/>.</summary>
    /// <remarks>
    /// UI Automation exposes no name for a button with a glyph and text, so the button is the one around its own
    /// text, level with the row's title; it is clicked.
    /// </remarks>
    public void ClickBeside(string row, string name)
    {
        var title = Find(e => Type(e) == ControlType.Text && NameOf(e) == row, row).BoundingRectangle;
        var label = Descendants()
            .Where(e => Type(e) is ControlType.Text or ControlType.Button && NameOf(e) == name)
            .FirstOrDefault(e => e.BoundingRectangle.Top <= title.Bottom + 40
                && e.BoundingRectangle.Bottom >= title.Top)
            ?? throw new InvalidOperationException($"No {name} beside {row}.");
        var button = Type(label) == ControlType.Text ? ButtonAround(label) : label;

        if (!button.Properties.IsEnabled.ValueOrDefault)
        {
            throw new InvalidOperationException($"{name} beside {row} is disabled.");
        }

        ClickOn(button, name);
        Settle(2500);
    }

    /// <summary>Opens the list row whose name contains <paramref name="text"/>; UIA cannot raise ItemClick.</summary>
    /// <param name="settle">0 when a brief state follows, such as the Overview loading after a workspace opens.</param>
    public void ClickItem(string listName, string text, int settle = 4000)
    {
        var list = Find(e => Type(e) == ControlType.List && NameOf(e) == listName, listName);
        var row = Retry
            .WhileNull(
                () => list
                    .FindAllChildren(c => c.ByControlType(ControlType.ListItem))
                    .FirstOrDefault(r => NameOf(r).Contains(text, StringComparison.Ordinal)),
                _findTimeout,
                ignoreException: true)
            .Result ?? throw new InvalidOperationException($"{listName} has no row with {text}.");
        ClickOn(row, text);
        Settle(settle);
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
            string name = NameOf(item);
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

    /// <summary>Types into the text box with this name, or into it and on, as a person tabbing out would.</summary>
    /// <remarks>
    /// The text is set through UI Automation, which gives the box focus as it does; the studio's boxes take their
    /// value as it changes. Tabbing out moves focus to the next control without a key: after keyboard input XAML
    /// draws a focus ring wherever focus goes next, such as on Browse… or a button the page focuses.
    /// </remarks>
    /// <param name="settle">0 when a brief state follows, such as the preview that tabbing out starts.</param>
    public void Type(string name, string text, bool tabOut = false, int settle = 800)
    {
        var box = Find(e => Type(e) == ControlType.Edit && Matches(e, name), name);

        if (tabOut)
        {
            box.Focus();
        }

        box.Patterns.Value.Pattern.SetValue(text);

        if (tabOut)
        {
            NextTabStop(box).Focus();
        }

        Settle(settle);
    }

    /// <summary>The next control that takes focus, as Tab finds it in a form laid out in reading order.</summary>
    private AutomationElement NextTabStop(AutomationElement element)
    {
        var walker = _automation.TreeWalkerFactory.GetControlViewWalker();

        for (var next = walker.GetNextSibling(element); next is not null; next = walker.GetNextSibling(next))
        {
            if (next.Properties.IsKeyboardFocusable.ValueOrDefault)
            {
                return next;
            }
        }

        throw new InvalidOperationException($"Nothing takes focus after {NameOf(element)}.");
    }

    /// <summary>Expands the expander whose name starts with <paramref name="name"/>, or the first one.</summary>
    public void Expand(string? name = null)
    {
        var expanders = Retry
            .WhileEmpty(
                () => _window.FindAllDescendants(cf => cf.ByClassName(_expanderClass)),
                _findTimeout,
                ignoreException: true)
            .Result ?? [];
        var expander = expanders.FirstOrDefault(e => name is null || Matches(e, name))
            ?? throw new InvalidOperationException($"No expander {name}.");

        if (expander.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value is not ExpandCollapseState.Expanded)
        {
            expander.Patterns.ExpandCollapse.Pattern.Expand();
            Settle(1500);
        }
    }

    /// <param name="offset">Where to rest, from the element's top-left corner; its centre when null.</param>
    public void Hover(string automationId, Point? offset = null)
    {
        var element = Find(e => Id(e) == automationId, automationId);
        element.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
        var bounds = element.BoundingRectangle;
        var at = offset is { } o ? new Point(bounds.Left + o.X, bounds.Top + o.Y) : bounds.Center();
        Desktop.MovePointer(new Point(at.X - 20, at.Y));
        Desktop.MovePointer(at);
        Settle(_indicatorFade + 1000);
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

    /// <summary>Scrolls the scroll viewer that holds this element to an offset from its top.</summary>
    /// <remarks>UI Automation scrolls by percent; the viewport's share of the extent turns pixels into one.</remarks>
    public void ScrollTo(string automationId, double pixels)
    {
        for (var element = Find(e => Id(e) == automationId, automationId); element is not null; element = element.Parent)
        {
            if (element.Patterns.Scroll.TryGetPattern(out var scroll) && scroll.VerticallyScrollable.Value)
            {
                double viewport = element.BoundingRectangle.Height;
                double extent = viewport * 100 / scroll.VerticalViewSize.Value;
                double percent = Math.Clamp(pixels * 100 / (extent - viewport), 0, 100);
                scroll.SetScrollPercent(ScrollPatternConstants.NoScroll, percent);
                Settle(_indicatorFade);

                return;
            }
        }

        throw new InvalidOperationException($"Nothing scrolls {automationId}.");
    }

    /// <summary>Waits until an element with this name, or whose name starts with it, shows.</summary>
    public void WaitFor(string name, TimeSpan timeout)
    {
        EnsureRunning();
        _ = Retry
                .WhileNull(
                    () => _window.FindFirstDescendant(c => c.ByName(name))
                        ?? Descendants().FirstOrDefault(e => Starts(e, name)),
                    timeout,
                    TimeSpan.FromMilliseconds(500),
                    ignoreException: true)
                .Result
            ?? throw new TimeoutException($"{name} did not show within {timeout.TotalSeconds} s.");
    }

    /// <summary>Waits for a state that lasts a second or so, with one native query every few milliseconds.</summary>
    /// <remarks>A walk of the whole tree takes longer than a short scan, so a brief state is matched exactly.</remarks>
    public void WaitBriefly(Func<ConditionFactory, ConditionBase> condition, string what, TimeSpan timeout)
    {
        EnsureRunning();
        _ = Retry
                .WhileNull(() => _window.FindFirstDescendant(condition), timeout, _poll, ignoreException: true)
                .Result
            ?? throw new TimeoutException($"{what} did not show within {timeout.TotalSeconds} s.");
    }

    /// <summary>Waits until the title bar's Back is off, as once a page opened fresh has cleared the history.</summary>
    public void WaitBackOff()
    {
        bool off = Retry
            .WhileFalse(
                () => _window.FindFirstDescendant(c => c.ByAutomationId("PART_BackButton")) is { } back
                    && !back.Properties.IsEnabled.ValueOrDefault,
                _findTimeout,
                _poll,
                ignoreException: true)
            .Result;

        if (!off)
        {
            throw new TimeoutException($"Back is still on after {_findTimeout.TotalSeconds} s.");
        }
    }

    /// <summary>Waits until the button with this name can be pressed, as a person waits before pressing it.</summary>
    public void WaitEnabled(string name, TimeSpan timeout)
    {
        EnsureRunning();
        bool enabled = Retry
            .WhileFalse(
                () => Descendants().Any(e => IsAction(e) && Matches(e, name) && e.Properties.IsEnabled.ValueOrDefault),
                timeout,
                TimeSpan.FromMilliseconds(500),
                ignoreException: true)
            .Result;

        if (!enabled)
        {
            throw new TimeoutException($"{name} is still disabled after {timeout.TotalSeconds} s.");
        }
    }

    /// <summary>Saves the window as it is when a step fails, beside the captures but outside the manifest.</summary>
    public void SaveFailure(string step)
    {
        string folder = Path.Combine(_output, "failures");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"{_theme}-{Slug(step)}");
        FlaUICapture
            .Rectangle(Desktop.VisibleFrame(Handle))
            .ToFile(file + ".png");

        // What UI Automation saw, so a control the step looked for can be told from one that was not there.
        var elements = Descendants()
            .Where(e => Type(e) is not ControlType.Pane and not ControlType.Group || NameOf(e).Length > 0)
            .Select(e => $"{Type(e)}\t{Id(e)}\t{NameOf(e)}\t{e.Properties.BoundingRectangle.ValueOrDefault}");
        File.WriteAllLines(file + ".txt", elements);
    }

    /// <summary>Invokes the title bar's pane toggle, the WinUI TitleBar's template part.</summary>
    public void TogglePane()
    {
        Find(e => Id(e) == "PART_PaneToggleButton", "Toggle Navigation").Patterns.Invoke.Pattern.Invoke();
        Settle(2500);
    }

    /// <summary>Waits until no element called <paramref name="name"/> shows.</summary>
    /// <remarks>A walk that meets an element as the page replaces it throws; that poll counts as showing.</remarks>
    public void WaitGone(string name, TimeSpan timeout)
    {
        EnsureRunning();
        var clock = Stopwatch.StartNew();

        while (Shows(name))
        {
            if (clock.Elapsed > timeout)
            {
                throw new TimeoutException($"{name} still shows after {timeout.TotalSeconds} s.");
            }

            Thread.Sleep(_poll);
        }
    }

    /// <summary>Clicks a button with the pointer, as a person does.</summary>
    public void Click(string name)
    {
        ClickOn(Find(e => IsAction(e) && Matches(e, name), name), name);
        Settle(2500);
    }

    /// <remarks>
    /// A list's ScrollIntoView scrolls the list but not the page around it, and an element below the fold has an
    /// empty rectangle; focus scrolls every scroll viewer around it, and after pointer input it draws no focus ring.
    /// The pointer leaves the window after the click, so a state captured right after it shows nothing hovered.
    /// </remarks>
    private void ClickOn(AutomationElement element, string what)
    {
        element.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();

        if (!InWindow(element))
        {
            element.Focus();
            Settle(500);
        }

        if (!InWindow(element))
        {
            throw new InvalidOperationException($"{what} is not in the window to click.");
        }

        Desktop.Click(element.BoundingRectangle.Center());
        MovePointerOff();
    }

    private void MovePointerOff()
    {
        var frame = Desktop.VisibleFrame(Handle);
        Desktop.MovePointer(new Point(frame.Right + 40, frame.Bottom + 40));
    }

    private bool InWindow(AutomationElement element)
    {
        var bounds = element.BoundingRectangle;
        var window = Desktop.VisibleFrame(Handle);

        return !bounds.IsEmpty && window.Contains(bounds.Center());
    }

    public static string Slug(string name)
    {
        var slug = new StringBuilder();

        foreach (char c in name.ToLowerInvariant())
        {
            slug.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        string text = slug.ToString();

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
        string actual = NameOf(element);

        return actual == name || actual.StartsWith(name + ",", StringComparison.Ordinal);
    }

    private static bool Starts(AutomationElement element, string text)
    {
        return NameOf(element).StartsWith(text, StringComparison.Ordinal);
    }

    /// <summary>The smallest button whose box holds the text: the button a glyph-and-text label belongs to.</summary>
    private AutomationElement ButtonAround(AutomationElement text)
    {
        var centre = text.BoundingRectangle.Center();

        return Descendants()
            .Where(e => Type(e) == ControlType.Button && e.BoundingRectangle.Contains(centre))
            .OrderBy(e => e.BoundingRectangle.Width * e.BoundingRectangle.Height)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No button around {NameOf(text)}.");
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

    private void Save(string page, string state, Size size, CaptureImage image)
    {
        string file = Path.Combine(_output, $"{page}-{state}-{size.Width}x{size.Height}-{_theme}.png");
        image.ToFile(file);
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

    /// <remarks>
    /// Checked after the screen is taken: the state may have moved on, but it must not have ended. A brief state does
    /// not come back once it ends, so one sighting is enough; a walk that meets the page as it changes throws or finds
    /// nothing, so it is asked again.
    /// </remarks>
    private bool StillShows(Func<ConditionFactory, ConditionBase> shows)
    {
        return Retry
            .WhileFalse(
                () => _window.FindFirstDescendant(shows) is not null,
                _stillShowsTimeout,
                _poll,
                ignoreException: true)
            .Result;
    }

    private bool Shows(string name)
    {
        try
        {
            return _window.FindFirstDescendant(c => c.ByName(name)) is not null;
        }
        catch (COMException)
        {
            return true;
        }
    }

    private AutomationElement Find(Func<AutomationElement, bool> predicate, string what)
    {
        EnsureRunning();

        return Retry
            .WhileNull(
                () => Descendants().FirstOrDefault(predicate),
                _findTimeout,
                TimeSpan.FromMilliseconds(500),
                ignoreException: true)
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

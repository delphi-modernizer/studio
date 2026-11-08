using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace DelphiModernizer.Studio.Modules.Overview.UI.Views;

/// <summary>Lays out the flow diagram with the measurements in the handoff notes, wide and under 1100.</summary>
/// <remarks>
/// Wide: four columns of 160, 236, 160 and 206 with gaps up to 80. Narrow: the Delphi code beside the teams two to a
/// row, then the Principal Architect beside the sign-offs one row each, joined by an elbow labelled drafts.
/// </remarks>
public sealed partial class OverviewPage
{
    private const double _pad = 24;
    private const double _nodeHeight = 52;
    private const double _groupPadding = 8;
    private bool _wide;

    private void OnFlowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 0.5)
        {
            LayOutFlow(e.NewSize.Width);
        }
    }

    /// <remarks>
    /// The canvas draws a sign-off's name and pill 44 high in a 56 node, 8 from its top, and the next sign-off's second
    /// pixel of border as an inset shadow; the padding keeps both where they are drawn, whatever the border.
    /// </remarks>
    private void ShowNextGate()
    {
        var gates = new[] { Gate0, Gate1, Gate2, Gate3, Gate4 };

        for (int i = 0; i < gates.Length && i < ViewModel.Gates.Count; i++)
        {
            bool next = ViewModel.Gates[i].IsNext;
            gates[i].Style = (Style)Resources[next ? "NextFlowNodeStyle" : "FlowNodeStyle"];
            gates[i].Padding = (next, _wide) switch {
                (true, true) => new Thickness(11, 6, 11, 2),
                (true, false) => new Thickness(11, 6, 11, 6),
                (false, true) => new Thickness(12, 7, 12, 3),
                _ => new Thickness(12, 7, 12, 7),
            };
        }
    }

    private void LayOutFlow(double width)
    {
        Decorations.Children.Clear();
        double available = width - (2 * _pad);
        bool wide = available >= 900;
        _wide = wide;
        var teamTemplate = (DataTemplate)Resources[wide ? "TeamNodeTemplate" : "TeamNodeCompactTemplate"];
        var gateTemplate = (DataTemplate)Resources[wide ? "GateNodeTemplate" : "GateNodeOneLineTemplate"];

        foreach (var team in new[] { Team0, Team1, Team2, Team3, Team4, Team5 })
        {
            team.ContentTemplate = teamTemplate;
        }

        foreach (var gate in new[] { Gate0, Gate1, Gate2, Gate3, Gate4 })
        {
            gate.ContentTemplate = gateTemplate;
        }

        LegendInside.Visibility = wide ? Visibility.Visible : Visibility.Collapsed;
        LegendBelow.Visibility = wide ? Visibility.Collapsed : Visibility.Visible;
        BuildSubtitle.Visibility = wide ? Visibility.Collapsed : Visibility.Visible;
        BuildLayout.Padding = wide ? new Thickness(12, 6, 12, 6) : new Thickness(12, 8, 12, 8);
        Grid.SetRow(BuildPill, wide ? 1 : 0);
        Grid.SetColumn(BuildPill, wide ? 1 : 2);
        BuildPill.HorizontalAlignment = wide ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        Flow.Height = wide ? LayOutWide(available) : LayOutNarrow(available);
        ShowNextGate();
    }

    /// <returns>The diagram's height.</returns>
    private double LayOutWide(double available)
    {
        const double legacyWidth = 160;
        const double teamWidth = 236;
        const double architectWidth = 160;
        const double gateWidth = 206;
        const double big = 156;
        const double top = 48;
        const double teamGap = 12;
        const double gateHeight = 56;
        const double gateGap = 10;
        double columns = legacyWidth + teamWidth + architectWidth + gateWidth;
        double gap = Math.Min(80, Math.Floor((available - columns) / 3));
        double x0 = _pad + Math.Floor((available - columns - (3 * gap)) / 2);
        double x1 = x0 + legacyWidth + gap;
        double x2 = x1 + teamWidth + gap;
        double x3 = x2 + architectWidth + gap;
        double teamsHeight = (6 * _nodeHeight) + (5 * teamGap) + (2 * _groupPadding);
        double gatesHeight = (5 * gateHeight) + (4 * gateGap) + (2 * _groupPadding);
        double middle = top + (teamsHeight / 2);

        Phase3Text.Text = "Architect combines";
        Place(Phase1, x0, 18, legacyWidth, 20);
        Place(Phase2, x1, 18, teamWidth, 20);
        Place(Phase3, x2, 18, architectWidth, 20);
        Place(Phase4, x3, 18, gateWidth + 40, 20);
        Place(TeamsGroup, x1, top, teamWidth, teamsHeight);
        Place(GatesGroup, x3, top, gateWidth, gatesHeight);
        Place(LegacyNodeButton, x0, middle - (big / 2), legacyWidth, big);
        Place(ArchitectNodeButton, x2, middle - (big / 2), architectWidth, big);
        PlaceTeams(i => new Rect(
            x1 + _groupPadding,
            top + _groupPadding + (i * (_nodeHeight + teamGap)),
            teamWidth - (2 * _groupPadding),
            _nodeHeight));
        double gateCentre = x3 + (gateWidth / 2);
        PlaceGates(
            i => new Rect(
                x3 + _groupPadding,
                top + _groupPadding + (i * (gateHeight + gateGap)),
                gateWidth - (2 * _groupPadding),
                gateHeight),
            gateCentre);
        HorizontalLine(x0 + legacyWidth, x1, middle, "evidence");
        HorizontalLine(x1 + teamWidth, x2, middle, "drafts");
        HorizontalLine(x2 + architectWidth, x3, middle, "review");
        double buildTop = top + gatesHeight + 32;
        VerticalLine(gateCentre, top + gatesHeight, buildTop, arrow: true);
        Label(gateCentre + 8, top + gatesHeight + 8, 60, "unlocks", TextAlignment.Left);
        Place(BuildBox, x3, buildTop, gateWidth, 60);
        double height = buildTop + 60 + 20;
        Place(LegendInside, x0, height - 32, x3 - x0 - 40, 20);

        return height;
    }

    /// <returns>The diagram's height.</returns>
    private double LayOutNarrow(double available)
    {
        const double legacyWidth = 150;
        const double gap = 48;
        const double big = 172;
        const double top = 52;
        const double rowGap = 12;
        const double gateHeight = 44;
        const double gateGap = 12;
        double groupWidth = available - legacyWidth - gap;
        double x0 = _pad;
        double x1 = x0 + legacyWidth + gap;
        double columnWidth = Math.Floor((groupWidth - (2 * _groupPadding) - 12) / 2);
        double teamsHeight = (3 * _nodeHeight) + (2 * rowGap) + (2 * _groupPadding);
        double middle = top + (teamsHeight / 2);
        double groupCentre = x1 + (groupWidth / 2);

        Phase3Text.Text = "Architect";
        Place(Phase1, x0, 18, legacyWidth, 20);
        Place(Phase2, x1, 18, groupWidth, 20);
        Place(TeamsGroup, x1, top, groupWidth, teamsHeight);
        Place(LegacyNodeButton, x0, middle - (big / 2), legacyWidth, big);
        PlaceTeams(i => new Rect(
            x1 + _groupPadding + (i % 2 * (columnWidth + 12)),
            top + _groupPadding + (Math.Floor(i / 2d) * (_nodeHeight + rowGap)),
            columnWidth,
            _nodeHeight));
        HorizontalLine(x0 + legacyWidth, x1, middle, "evidence");

        double elbow = top + teamsHeight + 20;
        double down = x0 + legacyWidth - 14;
        VerticalLine(groupCentre, top + teamsHeight, elbow, arrow: false);
        Line(down, elbow, groupCentre - down + 1, 1);
        Label(groupCentre + 8, top + teamsHeight + 2, 50, "drafts", TextAlignment.Left);

        double top2 = elbow + 64;
        double gatesHeight = (5 * gateHeight) + (4 * gateGap) + (2 * _groupPadding);
        double middle2 = top2 + (gatesHeight / 2);
        double architectTop = middle2 - (big / 2);
        Place(Phase3, x0, top2 - 34, legacyWidth - 22, 20);
        Place(Phase4, x1, top2 - 34, groupWidth, 20);
        Place(GatesGroup, x1, top2, groupWidth, gatesHeight);
        Place(ArchitectNodeButton, x0, architectTop, legacyWidth, big);
        VerticalLine(down, elbow, architectTop, arrow: true);
        PlaceGates(
            i => new Rect(
                x1 + _groupPadding,
                top2 + _groupPadding + (i * (gateHeight + gateGap)),
                groupWidth - (2 * _groupPadding),
                gateHeight),
            groupCentre);
        HorizontalLine(x0 + legacyWidth, x1, middle2, "review");

        double buildTop = top2 + gatesHeight + 32;
        VerticalLine(groupCentre, top2 + gatesHeight, buildTop, arrow: true);
        Label(groupCentre + 8, top2 + gatesHeight + 8, 60, "unlocks", TextAlignment.Left);
        Place(BuildBox, x1, buildTop, groupWidth, 52);

        return buildTop + 52 + 24;
    }

    /// <remarks>
    /// The canvas draws a team's tooltip 236 wide, 12 in from the card's left edge and 6 below it, so each card's
    /// tooltip is placed against an empty anchor that wide and as tall as the card. Opened by the pointer, WinUI still
    /// centres it on the pointer across.
    /// </remarks>
    private void PlaceTeams(Func<int, Rect> at)
    {
        var teams = new[] { Team0, Team1, Team2, Team3, Team4, Team5 };

        for (int i = 0; i < teams.Length; i++)
        {
            var rect = at(i);
            Place(teams[i], rect.X, rect.Y, rect.Width, rect.Height);

            var anchor = new Rectangle { Width = 236, Height = rect.Height, IsHitTestVisible = false };
            Decorate(anchor, rect.X + 12, rect.Y);
            ToolTipService.SetPlacementTarget(teams[i], anchor);
        }
    }

    /// <summary>Places the sign-offs, with a line between each and the next.</summary>
    private void PlaceGates(Func<int, Rect> at, double centre)
    {
        var gates = new[] { Gate0, Gate1, Gate2, Gate3, Gate4 };

        for (int i = 0; i < gates.Length; i++)
        {
            var rect = at(i);
            Place(gates[i], rect.X, rect.Y, rect.Width, rect.Height);

            if (i < gates.Length - 1)
            {
                VerticalLine(centre, rect.Bottom + 2, at(i + 1).Y - 2, arrow: false);
            }
        }
    }

    /// <summary>An arrow from x1 to x2, with its caption 22 above the line.</summary>
    private void HorizontalLine(double x1, double x2, double y, string caption)
    {
        Line(x1, y, x2 - x1 - 6, 1);
        Head(x2 - 7, y - 5, new Point(0, 0), new Point(7, 5), new Point(0, 10));
        double width = Math.Max(x2 - x1, 60);
        Label(Math.Round((x1 + x2 - width) / 2), y - 22, width, caption, TextAlignment.Center);
    }

    private void VerticalLine(double x, double y1, double y2, bool arrow)
    {
        Line(x, y1, 1, y2 - y1 - (arrow ? 6 : 0));

        if (arrow)
        {
            Head(x - 5, y2 - 7, new Point(0, 0), new Point(10, 0), new Point(5, 7));
        }
    }

    private void Line(double x, double y, double width, double height)
    {
        var line = new Rectangle {
            Width = Math.Max(width, 0),
            Height = Math.Max(height, 0),
            Style = (Style)Resources["FlowLineStyle"],
        };
        Decorate(line, x, y);
    }

    private void Head(double x, double y, params Point[] points)
    {
        var head = new Polygon { Style = (Style)Resources["FlowHeadStyle"] };

        foreach (var point in points)
        {
            head.Points.Add(point);
        }

        Decorate(head, x, y);
    }

    private void Label(double x, double y, double width, string text, TextAlignment alignment)
    {
        var label = new TextBlock {
            Text = text,
            Width = width,
            TextAlignment = alignment,
            Style = (Style)Resources["FlowLabelStyle"],
        };
        Decorate(label, x, y);
    }

    private void Decorate(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        Decorations.Children.Add(element);
    }

    private static void Place(FrameworkElement element, double x, double y, double width, double height)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        element.Width = Math.Max(width, 0);
        element.Height = Math.Max(height, 0);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Traceability.UI.Views;

/// <summary>The Traceability page.</summary>
public sealed partial class TraceabilityPage : Page, IHasViewModel
{
    public TraceabilityPage(TraceabilityViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChanged;
        ProblemsList.ContainerContentChanging += (_, args) => NameRow(args);
        MatrixList.ContainerContentChanging += (_, args) => NameRow(args);

        // A list owns its scrolling, no taller than the page, so hundreds of rows stay virtualised.
        PageScroller.SizeChanged += (_, args) => {
            ProblemsList.MaxHeight = args.NewSize.Height;
            MatrixList.MaxHeight = args.NewSize.Height;
        };
    }

    public TraceabilityViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.CheckCommand.ExecuteAsync(parameter: null);
    }

    /// <summary>The selector bars are rebuilt with counts after every check, keeping the person's choice.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TraceabilityViewModel.Views))
        {
            Fill(
                ViewBar,
                ViewModel.Views.Select(v => (v.Label, v.Count, v.AnnouncedName, (object)v.Value)),
                ViewModel.View);
        }

        if (e.PropertyName == nameof(TraceabilityViewModel.Severities))
        {
            Fill(
                SeverityBar,
                ViewModel.Severities.Select(v => (v.Label, v.Count, v.AnnouncedName, (object)v.Value)),
                ViewModel.Severity);
        }
    }

    private void OnViewChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is TraceView view)
        {
            ViewModel.View = view;
        }
    }

    private void OnSeverityChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is SeverityFilter severity)
        {
            ViewModel.Severity = severity;
        }
    }

    /// <summary>Enter or a click on a row opens its file at the line, as its own link does.</summary>
    private async void OnRowClicked(object sender, ItemClickEventArgs e)
    {
        var (path, line) = e.ClickedItem switch {
            ProblemRow row => (row.Path, row.Line),
            MatrixRow row => (row.Path, row.Line),
            _ => (null, 0),
        };

        if (path is not null)
        {
            await ViewModel.OpenAsync(path, line);
        }
    }

    private async void OnProblemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: ProblemRow problem })
        {
            await ViewModel.OpenAsync(problem.Path, problem.Line);
        }
    }

    private async void OnRequirementClicked(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: MatrixRow row })
        {
            await ViewModel.OpenAsync(row.Path, row.Line);
        }
    }

    /// <summary>A row names its container, which is what Narrator reads as the arrow keys move.</summary>
    private static void NameRow(ContainerContentChangingEventArgs args)
    {
        string? name = args.Item switch {
            ProblemRow row => row.AnnouncedName,
            MatrixRow row => row.AnnouncedName,
            _ => null,
        };

        if (name is not null)
        {
            AutomationProperties.SetName(args.ItemContainer, name);
        }
    }

    private static void Fill(
        SelectorBar bar,
        IEnumerable<(string Label, string Count, string Name, object Value)> choices,
        object selected)
    {
        bar.Items.Clear();

        foreach (var (label, count, name, value) in choices)
        {
            var item = new SelectorBarItem { Child = Counted(label, count), Tag = value };
            AutomationProperties.SetName(item, name);
            bar.Items.Add(item);
        }

        bar.SelectedItem = bar.Items.FirstOrDefault(i => Equals(i.Tag, selected));
    }

    /// <summary>A choice's label, then its count in smaller secondary text, as the design draws it.</summary>
    private static StackPanel Counted(string label, string count)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new TextBlock {
            Text = count,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["SecondaryCaptionTextStyle"],
        });

        return panel;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
    }

    public TraceabilityViewModel ViewModel { get; }

    DelphiModernizer.Studio.UI.Contracts.ViewModel IHasViewModel.ViewModel => ViewModel;

    /// <summary>The brush of a stat's status glyph; x:Bind calls it from the stat template.</summary>
    public static Brush ToneBrush(StatTone tone)
    {
        var key = tone switch {
            StatTone.Success => "SystemFillColorSuccessBrush",
            StatTone.Caution => "SystemFillColorCautionBrush",
            StatTone.Critical => "SystemFillColorCriticalBrush",
            _ => "TextFillColorSecondaryBrush",
        };

        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.CheckCommand.ExecuteAsync(parameter: null);
    }

    /// <summary>The selector bars are rebuilt with counts after every check, keeping the person's choice.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TraceabilityViewModel.Views))
        {
            Fill(ViewBar, ViewModel.Views.Select(v => (v.Text, v.AnnouncedName, (object)v.Value)), ViewModel.View);
        }

        if (e.PropertyName == nameof(TraceabilityViewModel.Severities))
        {
            Fill(
                SeverityBar,
                ViewModel.Severities.Select(v => (v.Text, v.AnnouncedName, (object)v.Value)),
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
        var name = args.Item switch {
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
        IEnumerable<(string Text, string Name, object Value)> choices,
        object selected)
    {
        bar.Items.Clear();

        foreach (var (text, name, value) in choices)
        {
            var item = new SelectorBarItem { Text = text, Tag = value };
            AutomationProperties.SetName(item, name);
            bar.Items.Add(item);
        }

        bar.SelectedItem = bar.Items.FirstOrDefault(i => Equals(i.Tag, selected));
    }
}

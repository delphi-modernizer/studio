using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Xaml;
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

    /// <summary>The selector bars are rebuilt with their counts after every check, keeping the person's choice.</summary>
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TraceabilityViewModel.Views))
        {
            Fill(ViewBar, ViewModel.Views.Select(v => (v.Text, (object)v.Value)), ViewModel.View);
        }

        if (e.PropertyName == nameof(TraceabilityViewModel.Severities))
        {
            Fill(SeverityBar, ViewModel.Severities.Select(v => (v.Text, (object)v.Value)), ViewModel.Severity);
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

    private static void Fill(SelectorBar bar, IEnumerable<(string Text, object Value)> choices, object selected)
    {
        bar.Items.Clear();

        foreach (var (text, value) in choices)
        {
            bar.Items.Add(new SelectorBarItem { Text = text, Tag = value });
        }

        bar.SelectedItem = bar.Items.FirstOrDefault(i => Equals(i.Tag, selected));
    }
}

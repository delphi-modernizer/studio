using System;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>How serious a navigation badge's count is, which decides its InfoBadge style.</summary>
public enum BadgeTone
{
    /// <summary>Something is broken, such as problems in the documents.</summary>
    Critical,

    /// <summary>Something is happening, such as runs in progress.</summary>
    Attention,
}

/// <summary>A count shown on a navigation item, such as the problems in the documents.</summary>
public interface IBadgeSource
{
    BadgeTone Tone { get; }

    /// <summary>The count now; zero hides the badge.</summary>
    int Count { get; }

    /// <summary>Raised on any thread when <see cref="Count"/> changes.</summary>
    event EventHandler? Changed;

    /// <summary>What Narrator reads after the item's name, such as "2 problems in the documents".</summary>
    string Describe(int count);
}

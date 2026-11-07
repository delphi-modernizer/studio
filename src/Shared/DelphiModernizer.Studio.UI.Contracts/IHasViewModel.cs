namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>A page whose view model receives the navigation parameter.</summary>
public interface IHasViewModel
{
    ViewModel ViewModel { get; }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace XwaInstallerLibCore;

/// <summary>
/// A single page inside a <see cref="Wizard"/>. Mirrors the API of the WPF
/// Xceed <c>WizardPage</c> subset that the installer relies on.
/// </summary>
public class WizardPage : HeaderedContentControl
{
    public static readonly StyledProperty<WizardPage?> NextPageProperty =
        AvaloniaProperty.Register<WizardPage, WizardPage?>(nameof(NextPage));

    public static readonly StyledProperty<WizardPage?> PreviousPageProperty =
        AvaloniaProperty.Register<WizardPage, WizardPage?>(nameof(PreviousPage));

    public static readonly StyledProperty<bool> CanSelectNextPageProperty =
        AvaloniaProperty.Register<WizardPage, bool>(nameof(CanSelectNextPage), true);

    public static readonly StyledProperty<bool> CanFinishProperty =
        AvaloniaProperty.Register<WizardPage, bool>(nameof(CanFinish));

    public static readonly StyledProperty<bool> CanCancelProperty =
        AvaloniaProperty.Register<WizardPage, bool>(nameof(CanCancel), true);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<WizardPage, string?>(nameof(Description));

    public static readonly StyledProperty<IBrush?> ExteriorPanelBackgroundProperty =
        AvaloniaProperty.Register<WizardPage, IBrush?>(nameof(ExteriorPanelBackground));

    public static readonly StyledProperty<bool> IsFinishPageProperty =
        AvaloniaProperty.Register<WizardPage, bool>(nameof(IsFinishPage));

    public static readonly StyledProperty<WizardPageButtonVisibility> CancelButtonVisibilityProperty =
        AvaloniaProperty.Register<WizardPage, WizardPageButtonVisibility>(nameof(CancelButtonVisibility), WizardPageButtonVisibility.Visible);

    public static readonly StyledProperty<WizardPageButtonVisibility> BackButtonVisibilityProperty =
        AvaloniaProperty.Register<WizardPage, WizardPageButtonVisibility>(nameof(BackButtonVisibility), WizardPageButtonVisibility.Visible);

    public event EventHandler? Enter;

    public event EventHandler? Leave;

    public WizardPage? NextPage
    {
        get => GetValue(NextPageProperty);
        set => SetValue(NextPageProperty, value);
    }

    public WizardPage? PreviousPage
    {
        get => GetValue(PreviousPageProperty);
        set => SetValue(PreviousPageProperty, value);
    }

    public bool CanSelectNextPage
    {
        get => GetValue(CanSelectNextPageProperty);
        set => SetValue(CanSelectNextPageProperty, value);
    }

    public bool CanFinish
    {
        get => GetValue(CanFinishProperty);
        set => SetValue(CanFinishProperty, value);
    }

    public bool CanCancel
    {
        get => GetValue(CanCancelProperty);
        set => SetValue(CanCancelProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public IBrush? ExteriorPanelBackground
    {
        get => GetValue(ExteriorPanelBackgroundProperty);
        set => SetValue(ExteriorPanelBackgroundProperty, value);
    }

    public bool IsFinishPage
    {
        get => GetValue(IsFinishPageProperty);
        set => SetValue(IsFinishPageProperty, value);
    }

    public WizardPageButtonVisibility CancelButtonVisibility
    {
        get => GetValue(CancelButtonVisibilityProperty);
        set => SetValue(CancelButtonVisibilityProperty, value);
    }

    public WizardPageButtonVisibility BackButtonVisibility
    {
        get => GetValue(BackButtonVisibilityProperty);
        set => SetValue(BackButtonVisibilityProperty, value);
    }

    public void RaiseEnter()
    {
        Enter?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseLeave()
    {
        Leave?.Invoke(this, EventArgs.Empty);
    }
}

public enum WizardPageButtonVisibility
{
    Visible,
    Hidden
}

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;

namespace XwaInstallerLibCore;

/// <summary>
/// A lightweight wizard control that replaces the WPF Xceed <c>Wizard</c>.
/// It hosts <see cref="WizardPage"/> children and provides Back/Next/Finish/Cancel
/// navigation with the same semantics the installer expects.
/// </summary>
public class Wizard : TemplatedControl
{
    public static readonly StyledProperty<WizardPage?> CurrentPageProperty =
        AvaloniaProperty.Register<Wizard, WizardPage?>(nameof(CurrentPage));

    public static readonly StyledProperty<object?> FinishButtonContentProperty =
        AvaloniaProperty.Register<Wizard, object?>(nameof(FinishButtonContent), "Finish");

    public static readonly StyledProperty<object?> NextButtonContentProperty =
        AvaloniaProperty.Register<Wizard, object?>(nameof(NextButtonContent), "Next");

    public static readonly StyledProperty<object?> BackButtonContentProperty =
        AvaloniaProperty.Register<Wizard, object?>(nameof(BackButtonContent), "Back");

    public static readonly StyledProperty<object?> CancelButtonContentProperty =
        AvaloniaProperty.Register<Wizard, object?>(nameof(CancelButtonContent), "Cancel");

    public static readonly StyledProperty<WizardButtonVisibility> HelpButtonVisibilityProperty =
        AvaloniaProperty.Register<Wizard, WizardButtonVisibility>(nameof(HelpButtonVisibility), WizardButtonVisibility.Hidden);

    public static readonly StyledProperty<WizardButtonVisibility> CancelButtonVisibilityProperty =
        AvaloniaProperty.Register<Wizard, WizardButtonVisibility>(nameof(CancelButtonVisibility), WizardButtonVisibility.Visible);

    public static readonly StyledProperty<WizardButtonVisibility> BackButtonVisibilityProperty =
        AvaloniaProperty.Register<Wizard, WizardButtonVisibility>(nameof(BackButtonVisibility), WizardButtonVisibility.Visible);

    public static readonly StyledProperty<WizardButtonVisibility> NextButtonVisibilityProperty =
        AvaloniaProperty.Register<Wizard, WizardButtonVisibility>(nameof(NextButtonVisibility), WizardButtonVisibility.Visible);

    public static readonly StyledProperty<WizardButtonVisibility> FinishButtonVisibilityProperty =
        AvaloniaProperty.Register<Wizard, WizardButtonVisibility>(nameof(FinishButtonVisibility), WizardButtonVisibility.Visible);

    public event EventHandler<WizardCancelEventArgs>? Finish;

    public event EventHandler<WizardCancelEventArgs>? Cancel;

    public event EventHandler<WizardCancelEventArgs>? Next;

    public event EventHandler<WizardCancelEventArgs>? Back;

    public ObservableCollection<WizardPage> Pages { get; } = new();

    public WizardPage? CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public object? FinishButtonContent
    {
        get => GetValue(FinishButtonContentProperty);
        set => SetValue(FinishButtonContentProperty, value);
    }

    public object? NextButtonContent
    {
        get => GetValue(NextButtonContentProperty);
        set => SetValue(NextButtonContentProperty, value);
    }

    public object? BackButtonContent
    {
        get => GetValue(BackButtonContentProperty);
        set => SetValue(BackButtonContentProperty, value);
    }

    public object? CancelButtonContent
    {
        get => GetValue(CancelButtonContentProperty);
        set => SetValue(CancelButtonContentProperty, value);
    }

    public WizardButtonVisibility HelpButtonVisibility
    {
        get => GetValue(HelpButtonVisibilityProperty);
        set => SetValue(HelpButtonVisibilityProperty, value);
    }

    public WizardButtonVisibility CancelButtonVisibility
    {
        get => GetValue(CancelButtonVisibilityProperty);
        set => SetValue(CancelButtonVisibilityProperty, value);
    }

    public WizardButtonVisibility BackButtonVisibility
    {
        get => GetValue(BackButtonVisibilityProperty);
        set => SetValue(BackButtonVisibilityProperty, value);
    }

    public WizardButtonVisibility NextButtonVisibility
    {
        get => GetValue(NextButtonVisibilityProperty);
        set => SetValue(NextButtonVisibilityProperty, value);
    }

    public WizardButtonVisibility FinishButtonVisibility
    {
        get => GetValue(FinishButtonVisibilityProperty);
        set => SetValue(FinishButtonVisibilityProperty, value);
    }

    public bool CanGoBack => CurrentPage is not null && WizardHelpers.GetPreviousPage(this, CurrentPage) is not null;

    public bool CanGoNext => CurrentPage is not null && CurrentPage.CanSelectNextPage && WizardHelpers.GetNextPage(this, CurrentPage) is not null;

    public bool IsOnFinishPage => CurrentPage is not null && (CurrentPage.IsFinishPage || CurrentPage.CanFinish || WizardHelpers.GetNextPage(this, CurrentPage) is null);

    public void GoBack()
    {
        if (CurrentPage is null)
        {
            return;
        }

        var args = new WizardCancelEventArgs();

        Back?.Invoke(this, args);

        if (args.Cancel)
        {
            return;
        }

        WizardPage? previous = WizardHelpers.GetPreviousPage(this, CurrentPage);

        if (previous is not null)
        {
            CurrentPage = previous;
        }
    }

    public void GoNext()
    {
        if (CurrentPage is null || !CurrentPage.CanSelectNextPage)
        {
            return;
        }

        var args = new WizardCancelEventArgs();

        Next?.Invoke(this, args);

        if (args.Cancel)
        {
            return;
        }

        WizardPage? next = WizardHelpers.GetNextPage(this, CurrentPage);

        if (next is not null)
        {
            CurrentPage = next;
        }
    }

    public void FinishWizard()
    {
        var args = new WizardCancelEventArgs();

        Finish?.Invoke(this, args);

        if (args.Cancel)
        {
            return;
        }

        if (this.FindAncestorOfType<Window>() is { } window)
        {
            window.Close();
        }
    }

    public void CancelWizard()
    {
        var args = new WizardCancelEventArgs();

        Cancel?.Invoke(this, args);

        if (args.Cancel)
        {
            return;
        }

        if (this.FindAncestorOfType<Window>() is { } window)
        {
            window.Close();
        }
    }

    private Button? _backButton;
    private Button? _nextButton;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _backButton = e.NameScope.Find<Button>("PART_BackButton");
        _nextButton = e.NameScope.Find<Button>("PART_NextButton");

        if (_backButton is not null)
        {
            _backButton.Click += (_, _) => GoBack();
        }

        if (_nextButton is not null)
        {
            _nextButton.Click += (_, _) => GoNext();
        }

        if (e.NameScope.Find<Button>("PART_FinishButton") is { } finishButton)
        {
            finishButton.Click += (_, _) => FinishWizard();
        }

        if (e.NameScope.Find<Button>("PART_CancelButton") is { } cancelButton)
        {
            cancelButton.Click += (_, _) => CancelWizard();
        }

        UpdateNavigationState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentPageProperty)
        {
            if (change.OldValue is WizardPage oldPage)
            {
                oldPage.RaiseLeave();
            }

            if (change.NewValue is WizardPage newPage)
            {
                newPage.RaiseEnter();
            }

            UpdateNavigationState();
        }
    }

    private void UpdateNavigationState()
    {
        if (_backButton is not null)
        {
            _backButton.IsEnabled = CanGoBack;
        }

        if (_nextButton is not null)
        {
            _nextButton.IsEnabled = CanGoNext;
        }
    }
}

public enum WizardButtonVisibility
{
    Visible,
    Hidden
}

public sealed class WizardCancelEventArgs : RoutedEventArgs
{
    public bool Cancel { get; set; }
}

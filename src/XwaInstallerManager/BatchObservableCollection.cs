using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace XwaInstallerManager;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can add many items while raising
/// a single <see cref="INotifyCollectionChanged.CollectionChanged"/> notification.
/// </summary>
/// <remarks>
/// Adding items one at a time makes a bound DataGrid re-lay out for every row,
/// which costs seconds per row on a large list. <see cref="AddRange"/> defers the
/// notification until the whole batch is in place.
/// </remarks>
public sealed class BatchObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification;

    public void AddRange(IEnumerable<T> items)
    {
        if (items is null)
        {
            return;
        }

        _suppressNotification = true;

        try
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        // A single Reset tells the view to rebuild once.
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }
}

using Flow.Diagrams.Core.Models.Base;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Flow.Diagrams.Core.Layers;

/// <summary>
/// The collection of one kind of model in a <see cref="Diagram"/> (nodes, links or groups). It is a read-only list;
/// change it through the add and remove methods, which batch their refreshes and raise <see cref="Added"/> and <see
/// cref="Removed"/>.
/// </summary>
public abstract class BaseLayer<T> : IReadOnlyList<T> where T : Model
{
    private readonly List<T> _items = new List<T>();

    /// <summary>Raised for each item after it was added and <see cref="OnItemAdded"/> ran.</summary>
    public event Action<T>? Added;
    /// <summary>Raised for each item after it was removed and <see cref="OnItemRemoved"/> ran.</summary>
    public event Action<T>? Removed;

    /// <summary>Creates an empty layer in <paramref name="diagram"/>.</summary>
    public BaseLayer(Diagram diagram)
    {
        Diagram = diagram;
    }

    /// <summary>
    /// Adds <paramref name="item"/> and returns it, so creation and addition can be one expression. Throws <see
    /// cref="ArgumentNullException"/> for <c>null</c>; the same item is not rejected if already present.
    /// </summary>
    public virtual TSpecific Add<TSpecific>(TSpecific item) where TSpecific : T
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        Diagram.Batch(() =>
        {
            _items.Add(item);
            item.AttachTracker(Diagram.Changes);
            OnItemAdded(item);
            Added?.Invoke(item);
        });
        return item;
    }

    /// <summary>Adds every item in one batch, so the diagram refreshes once.</summary>
    public virtual void Add(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        Diagram.Batch(() =>
        {
            foreach (var item in items)
            {
                _items.Add(item);
                item.AttachTracker(Diagram.Changes);
                OnItemAdded(item);
                Added?.Invoke(item);
            }
        });
    }

    /// <summary>Removes <paramref name="item"/>; does nothing if it is not in the layer.</summary>
    public virtual void Remove(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (_items.Remove(item))
        {
            Diagram.Batch(() =>
            {
                OnItemRemoved(item);
                item.AttachTracker(null);
                Removed?.Invoke(item);
            });
        }
    }

    /// <summary>Removes every item that is in the layer, in one batch.</summary>
    public virtual void Remove(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        Diagram.Batch(() =>
        {
            foreach (var item in items)
            {
                if (_items.Remove(item))
                {
                    OnItemRemoved(item);
                    item.AttachTracker(null);
                    Removed?.Invoke(item);
                }
            }
        });
    }

    /// <summary>Whether <paramref name="item"/> is in the layer.</summary>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>Removes every item, last first, in one batch.</summary>
    public void Clear()
    {
        if (Count == 0)
            return;

        Diagram.Batch(() =>
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                _items.RemoveAt(i);
                OnItemRemoved(item);
                item.AttachTracker(null);
                Removed?.Invoke(item);
            }
        });
    }

    /// <summary>
    /// Hook called for each added item, before <see cref="Added"/>; subclasses attach the item to the diagram here.
    /// </summary>
    protected virtual void OnItemAdded(T item) { }

    /// <summary>
    /// Hook called for each removed item, before <see cref="Removed"/>; subclasses detach the item from the diagram
    /// here.
    /// </summary>
    protected virtual void OnItemRemoved(T item) { }

    /// <summary>The diagram this layer belongs to.</summary>
    public Diagram Diagram { get; }

    /// <summary>The number of items in the layer.</summary>
    public int Count => _items.Count;
    /// <summary>The item at <paramref name="index"/>, in the order added.</summary>
    public T this[int index] => _items[index];
    /// <summary>Enumerates the items in the order added; do not add or remove items during the enumeration.</summary>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

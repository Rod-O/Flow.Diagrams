using Flow.Diagrams.Core.ChangeTracking;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Models;

/// <summary>A text label placed along a link's path.</summary>
public class LinkLabelModel : Model
{
    private string _content;
    private double? _distance;
    private Point? _offset;

    /// <summary>
    /// Creates a label with the given <paramref name="id"/>. It is not added to the link; use <see
    /// cref="BaseLinkModel.AddLabel"/> to create and add one.
    /// </summary>
    public LinkLabelModel(BaseLinkModel parent, string id, string content, double? distance = null, Point? offset = null) : base(id)
    {
        Parent = parent;
        _content = content;
        _distance = distance;
        _offset = offset;
    }

    /// <summary>
    /// Creates a label with a generated id. It is not added to the link; use <see cref="BaseLinkModel.AddLabel"/> to
    /// create and add one.
    /// </summary>
    public LinkLabelModel(BaseLinkModel parent, string content, double? distance = null, Point? offset = null)
    {
        Parent = parent;
        _content = content;
        _distance = distance;
        _offset = offset;
    }

    /// <summary>The link the label belongs to.</summary>
    public BaseLinkModel Parent { get; }

    internal override ChangeTracker? Tracker => Parent.Tracker;
    /// <summary>The text of the label. A change raises <see cref="Model.Changed"/> with <see cref="ModelChanges.Appearance"/>.</summary>
    public string Content
    {
        get => _content;
        set => SetField(ref _content, value, ModelChanges.Appearance);
    }
    /// <summary>
    /// 3 types of values are possible:
    /// <para>- A number between 0 and 1: Position relative to the link's length</para>
    /// <para>- A positive number, greater than 1: Position away from the start</para>
    /// <para>- A negative number, less than 0: Position away from the end</para>
    /// A change raises <see cref="Model.Changed"/> with <see cref="ModelChanges.Appearance"/>.
    /// </summary>
    public double? Distance
    {
        get => _distance;
        set => SetField(ref _distance, value, ModelChanges.Appearance);
    }
    /// <summary>
    /// Displacement from the position given by <see cref="Distance"/>, in diagram units; <c>null</c> for none. A
    /// change raises <see cref="Model.Changed"/> with <see cref="ModelChanges.Appearance"/>.
    /// </summary>
    public Point? Offset
    {
        get => _offset;
        set => SetField(ref _offset, value, ModelChanges.Appearance);
    }
}

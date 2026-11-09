using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Behaviors;

/// <summary>
/// Hides models that lie outside the visible area by setting <see cref="Models.Base.Model.Visible"/>, per the kinds enabled in
/// <see cref="Options.DiagramOptions.Virtualization"/>. Rechecked when the zoom, pan or container size changes, when a
/// node, group or link is added, and when a drag ends (<see cref="Models.Base.MovableModel.Moved"/>). Registered by default;
/// does nothing while virtualization is disabled. It remembers the models it hid (<see cref="HiddenModels"/>): only those
/// are shown again, so a model hidden through <see cref="Models.Base.Model.Visible"/> stays hidden, and disabling
/// virtualization (or one of its kinds) shows them again at the next check; call <see cref="CheckVisibility()"/> to apply a
/// change of the options at once.
/// </summary>
public class VirtualizationBehavior : Behavior
{
    private readonly HashSet<Model> _hidden = new();

    /// <summary>Starts listening to zoom, pan, container and model changes of <paramref name="diagram"/>.</summary>
    public VirtualizationBehavior(Diagram diagram) : base(diagram)
    {
        Diagram.ZoomChanged += CheckVisibility;
        Diagram.PanChanged += CheckVisibility;
        Diagram.ContainerChanged += CheckVisibility;
        Diagram.Nodes.Added += OnAdded;
        Diagram.Groups.Added += OnAdded;
        Diagram.Links.Added += OnAdded;
        Diagram.Nodes.Removed += OnRemoved;
        Diagram.Groups.Removed += OnRemoved;
        Diagram.Links.Removed += OnRemoved;
    }

    /// <summary>The models this behavior has hidden because they lie outside the viewport; models hidden by the app are not listed.</summary>
    public IReadOnlyCollection<Model> HiddenModels => _hidden;

    /// <summary>
    /// Rechecks every model against the viewport: hides those outside it and shows those this behavior hid that are
    /// inside it, or all of them when virtualization (or their kind) is disabled.
    /// </summary>
    public void CheckVisibility()
    {
        var options = Diagram.Options.Virtualization;

        if (!options.Enabled || Diagram.Container == null)
        {
            if (!options.Enabled)
                ShowHidden(_ => true);
            return;
        }

        if (!options.OnNodes) ShowHidden(m => m is Models.NodeModel and not Models.GroupModel);
        if (!options.OnGroups) ShowHidden(m => m is Models.GroupModel);
        if (!options.OnLinks) ShowHidden(m => m is BaseLinkModel);

        if (options.OnNodes)
        {
            foreach (var node in Diagram.Nodes)
                CheckVisibility(node);
        }

        if (options.OnGroups)
        {
            foreach (var group in Diagram.Groups)
                CheckVisibility(group);
        }

        if (options.OnLinks)
        {
            foreach (var link in Diagram.Links)
                CheckVisibility(link);
        }
    }

    private void ShowHidden(Func<Model, bool> predicate)
    {
        foreach (var model in _hidden.Where(predicate).ToList())
        {
            _hidden.Remove(model);
            model.Visible = true;
        }
    }

    private void OnAdded(Model model)
    {
        if (model is MovableModel movable)
            movable.Moved += OnMoved;

        CheckVisibilityOf(model);
    }

    private void OnRemoved(Model model)
    {
        if (model is MovableModel movable)
            movable.Moved -= OnMoved;

        _hidden.Remove(model);
    }

    private void OnMoved(MovableModel model)
    {
        CheckVisibilityOf(model);
        // A link follows the nodes it joins without being moved itself.
        if (Diagram.Options.Virtualization.Enabled && Diagram.Options.Virtualization.OnLinks && Diagram.Container != null)
        {
            foreach (var link in Diagram.Links)
                CheckVisibility(link);
        }
    }

    private void CheckVisibilityOf(Model model)
    {
        var options = Diagram.Options.Virtualization;
        if (!options.Enabled || Diagram.Container == null)
            return;

        var covered = model switch
        {
            Models.GroupModel => options.OnGroups,
            Models.NodeModel => options.OnNodes,
            BaseLinkModel => options.OnLinks,
            _ => false,
        };
        if (covered)
            CheckVisibility(model);
    }

    private void CheckVisibility(Model model)
    {
        if (model is not IHasBounds ihb)
            return;

        var bounds = ihb.GetBounds();
        if (bounds == null)
            return;

        var left = bounds.Left * Diagram.Zoom + Diagram.Pan.X;
        var top = bounds.Top * Diagram.Zoom + Diagram.Pan.Y;
        var right = left + bounds.Width * Diagram.Zoom;
        var bottom = top + bounds.Height * Diagram.Zoom;
        var inside = right > 0 && left < Diagram.Container!.Width && bottom > 0 && top < Diagram.Container.Height;

        if (inside)
        {
            if (_hidden.Remove(model))
                model.Visible = true;
        }
        else if (model.Visible)
        {
            _hidden.Add(model);
            model.Visible = false;
        }
    }

    /// <summary>Stops listening to diagram changes.</summary>
    public override void Dispose()
    {
        Diagram.ZoomChanged -= CheckVisibility;
        Diagram.PanChanged -= CheckVisibility;
        Diagram.ContainerChanged -= CheckVisibility;
        Diagram.Nodes.Added -= OnAdded;
        Diagram.Groups.Added -= OnAdded;
        Diagram.Links.Added -= OnAdded;
        Diagram.Nodes.Removed -= OnRemoved;
        Diagram.Groups.Removed -= OnRemoved;
        Diagram.Links.Removed -= OnRemoved;
    }
}

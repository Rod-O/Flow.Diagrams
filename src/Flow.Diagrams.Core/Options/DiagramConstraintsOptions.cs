using System;
using System.Threading.Tasks;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Options;

/// <summary>
/// Veto hooks for deletions started by the user (the remove control and the Delete key); deleting through the API is
/// not affected.
/// </summary>
public class DiagramConstraintsOptions
{
    /// <summary>
    /// Called before a selected node is deleted; return <c>false</c> to keep it. Allows everything by default.
    /// </summary>
    public Func<NodeModel, ValueTask<bool>> ShouldDeleteNode { get; set; } = _ => ValueTask.FromResult(true);
    /// <summary>
    /// Called before a selected link is deleted; return <c>false</c> to keep it. Allows everything by default.
    /// </summary>
    public Func<BaseLinkModel, ValueTask<bool>> ShouldDeleteLink { get; set; } = _ => ValueTask.FromResult(true);
    /// <summary>
    /// Called before a selected group is deleted; return <c>false</c> to keep it. Allows everything by default.
    /// </summary>
    public Func<GroupModel, ValueTask<bool>> ShouldDeleteGroup { get; set; } = _ => ValueTask.FromResult(true);
}
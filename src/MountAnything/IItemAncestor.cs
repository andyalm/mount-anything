namespace MountAnything;

/// <summary>
/// When injected into a <see cref="IPathHandler"/>,
/// provides access to an ancestor <see cref="IItem"/> object in
/// the current path. This is useful to provide context to descendent
/// <see cref="IPathHandler"/>'s to understand the full context.
/// </summary>
/// <typeparam name="TItem">The concrete type of the <see cref="IItem"/> that is being retrieved.</typeparam>
public interface IItemAncestor<out TItem> where TItem : IItem
{
    TItem Item { get; }
}
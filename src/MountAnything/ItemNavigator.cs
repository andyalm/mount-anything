namespace MountAnything;

public abstract class ItemNavigator<TModel,TItem> where TItem : IItem
{
    protected abstract TItem CreateDirectoryItem(ItemPath parentPath, ItemPath directoryPath);
    protected abstract TItem CreateItem(ItemPath parentPath, TModel model);
    protected abstract ItemPath GetPath(TModel model);
    protected abstract IEnumerable<TModel> ListItems(ItemPath? pathPrefix);

    public IEnumerable<TItem> ListChildItems(ItemPath parentPath, ItemPath? pathPrefix = null)
    {
        var allModels = ListItems(pathPrefix).ToArray();
        var directories = Directories(allModels, pathPrefix).ToArray();
        var childRoles = Children(allModels, pathPrefix).ToArray();

        
        return directories.OrderBy(d => d.ToString())
            .Select(directory => CreateDirectoryItem(parentPath, pathPrefix.SafeCombine(directory)))
            .Concat(childRoles.Select(m => CreateItem(parentPath, m)).OrderBy(i => i.ItemName));
    }
    
    private IEnumerable<string> Directories(IEnumerable<TModel> models, ItemPath? pathPrefix)
    {
        string? childName = null;
        return (from obj in models
            let modelPath = GetPath(obj)
            where !modelPath.IsRoot && modelPath.Parent.IsAncestorOf(pathPrefix ?? ItemPath.Root, out childName)
            select childName).Distinct();
    }
    
    private IEnumerable<TModel> Children(IEnumerable<TModel> models, ItemPath? pathPrefix)
    {
        return from model in models
            let modelPath = GetPath(model)
            where modelPath.Parent.FullName.Equals(pathPrefix?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            select model;
    }
}
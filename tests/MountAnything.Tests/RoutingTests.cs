using System;
using System.Collections.Generic;
using System.Management.Automation;
using Autofac;
using FluentAssertions;
using MountAnything.Routing;
using Xunit;

namespace MountAnything.Tests;

public class RoutingTests
{
    private readonly Router _router;
    
    public RoutingTests()
    {
        _router = Router.Create<RootHandler>();
        _router.Map<TopLevelHandler>(topLevel =>
        {
            topLevel.Map<IntermediateHandler>(intermediate =>
            {
                intermediate.Map<LeafHandler>();
            });
        });
        
    }

    [Theory]
    [InlineData("toplevel01", typeof(TopLevelHandler))]
    [InlineData("toplevel01/intermediate01", typeof(IntermediateHandler))]
    [InlineData("toplevel02/intermediate02/leaf01", typeof(LeafHandler))]
    [InlineData("", typeof(RootHandler))]
    public void HandlerTypeCanBeResolvedFromPath(string path, Type expectedHandlerType)
    {
        _router.GetResolver(new ItemPath(path)).HandlerType.Should().Be(expectedHandlerType);
    }
    
    [Theory]
    [InlineData("toplevel01", typeof(TopLevelHandler))]
    [InlineData("toplevel01/intermediate01", typeof(IntermediateHandler))]
    [InlineData("toplevel02/intermediate02/leaf01", typeof(LeafHandler))]
    [InlineData("", typeof(RootHandler))]
    public void HandlerCanBeCreatedFromPath(string path, Type expectedHandlerType)
    {
        var (handler, lifetimeScope) = _router.RouteToHandler(new ItemPath(path), new FakeHandlerContext());
        try
        {
            handler.Should().BeOfType(expectedHandlerType);
        }
        finally
        {
            lifetimeScope.Dispose();
        }
    }

    [Theory]
    [InlineData("toplevel01/intermediate02")]
    [InlineData("toplevel01/intermediate03")]
    public void CanInjectItemFromParentHandler(string injectedItemPath)
    {
        var itemPath = new ItemPath(injectedItemPath).Combine("leaf02");
        var (_, lifetimeScope) = _router.RouteToHandler(itemPath, new FakeHandlerContext());
        try
        {
            lifetimeScope.Resolve<IntermediateItem>().ItemName.Should().Be(new ItemPath(injectedItemPath).Name);
        }
        finally
        {
            lifetimeScope.Dispose();
        }
    }
    
    private class RootHandler : PathHandler
    {
        public RootHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
        {
        }

        protected override IItem? GetItemImpl()
        {
            return new RootItem(ParentPath);
        }

        protected override IEnumerable<IItem> GetChildItemsImpl()
        {
            yield return new TopLevelItem(ParentPath, "toplevel01");
            yield return new TopLevelItem(ParentPath, "toplevel02");
        }
    }
    
    private class RootItem : Item
    {
        public RootItem(ItemPath parentPath) : base(parentPath, new PSObject())
        {
        }

        public override string ItemName => "";
        public override bool IsContainer => true;
    }
    
    public class TopLevelHandler : PathHandler
    {
        public TopLevelHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
        {
        }

        protected override IItem? GetItemImpl()
        {
            return new TopLevelItem(ParentPath, ItemName);
        }

        protected override IEnumerable<IItem> GetChildItemsImpl()
        {
            yield return new IntermediateItem(Path, "intermediate01");
            yield return new IntermediateItem(Path, "intermediate02");
            yield return new IntermediateItem(Path, "intermediate03");
        }
    }
    
    public class TopLevelItem : Item
    {
        public TopLevelItem(ItemPath parentPath, string itemName) : base(parentPath, new PSObject())
        {
            ItemName = itemName;
        }

        public override string ItemName { get; }
        public override bool IsContainer => true;
    }
    
    public class IntermediateHandler : PathHandler
    {
        public IntermediateHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
        {
        }

        protected override IItem? GetItemImpl()
        {
            return new IntermediateItem(ParentPath, ItemName);
        }

        protected override IEnumerable<IItem> GetChildItemsImpl()
        {
            yield return new LeafItem(Path, "leaf01");
            yield return new LeafItem(Path, "leaf02");
            yield return new LeafItem(Path, "leaf03");
        }
    }
    
    private class IntermediateItem : Item
    {
        public IntermediateItem(ItemPath parentPath, string itemName) : base(parentPath, new PSObject())
        {
            ItemName = itemName;
        }

        public override string ItemName { get; }
        public override bool IsContainer => true;
    }
    
    private class LeafHandler : PathHandler
    {
        public LeafHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
        {
        }

        protected override IItem? GetItemImpl()
        {
            return new LeafItem(ParentPath, ItemName);
        }

        protected override IEnumerable<IItem> GetChildItemsImpl()
        {
            yield break;
        }
    }
    
    private class LeafItem : Item
    {
        public LeafItem(ItemPath parentPath, string itemName) : base(parentPath, new PSObject())
        {
            ItemName = itemName;
        }

        public override string ItemName { get; }
        public override bool IsContainer => false;
    }
    
    private class FakeHandlerContext : IPathHandlerContext
    {
        public Cache Cache { get; } = new();
        public void WriteDebug(string message)
        {
            
        }

        public void WriteWarning(string message)
        {
            
        }

        public bool Force { get; set; }
        
        public CommandInvocationIntrinsics InvokeCommand { get; } = null!;
    }
}
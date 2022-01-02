using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using FluentAssertions;
using Xunit;

namespace MountAnything.Tests;

public class ItemTests
{
    [Fact]
    public void FirstTypeNameIsOnlyTypeOnFinalPSObject()
    {
        var underlyingObject = new PSObject();
        underlyingObject.TypeNames.Clear();
        underlyingObject.TypeNames.Add("MyType");
        underlyingObject.TypeNames.Add("AnotherType");

        var item = new TestItem(ItemPath.Root, underlyingObject);
        var pipelineObject = item.ToPipelineObject(p => p.ToString());

        pipelineObject.TypeNames.Should().HaveCount(1);
        pipelineObject.TypeNames.Single().Should().Be("MyType");
    }

    [Fact]
    public void CanGetStringPropertyOfUnderlyingObject()
    {
        var item = new TestItem(ItemPath.Root, new
        {
            MyProperty = "testvalue"
        });

        item.Property<string>("MyProperty").Should().Be("testvalue");
    }
    
    [Fact]
    public void CanGetConvertedPropertyOfUnderlyingObject()
    {
        var item = new TestItem(ItemPath.Root, new
        {
            MyProperty = "123"
        });

        item.Property<int>("MyProperty").Should().Be(123);
    }

    [Fact]
    public void CanGetComplexEnumerablePropertyAsEnumerableOfPSObjects()
    {
        var item = new TestItem(ItemPath.Root, new
        {
            ComplexChildren = new[]
            {
                new
                {
                    ChildProp = "child-value-1"
                },
                new
                {
                    ChildProp = "child-value-2"
                }
            }
        });

        var complexChildren = item.Property<IEnumerable<PSObject>>("ComplexChildren")!.ToArray();
        complexChildren.Should().HaveCount(2);
        complexChildren[0].Property<string>("ChildProp").Should().Be("child-value-1");
        complexChildren[1].Property<string>("ChildProp").Should().Be("child-value-2");
    }

    [Fact]
    public void ItemTypePropertyOverridesUnderlyingValue()
    {
        var item = new ItemWithCustomItemType("over", new
        {
            ItemType = "under"
        });
        var pipelineObject = item.ToPipelineObject(p => p.ToString());
        pipelineObject.Properties["ItemType"].Value.Should().Be("over");
    }

    public class TestItem : Item
    {
        public TestItem(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject) {}
        
        public TestItem(ItemPath parentPath, object underlyingObject) : base(parentPath, underlyingObject) {}

        public override string ItemName => "Test";
        public override bool IsContainer => false;

        public new T? Property<T>(string propertyName) => base.Property<T>(propertyName);
    }

    public class ItemWithCustomItemType : Item
    {
        public ItemWithCustomItemType(string itemType, object underlyingObject) : base(ItemPath.Root, underlyingObject)
        {
            ItemType = itemType;
        }

        public override string ItemName => "Test";
        public override string ItemType { get; }
        public override bool IsContainer => false;
    }
}
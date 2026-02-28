using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using FluentAssertions;
using Xunit;

namespace MountAnything.Tests;

public class ItemTests
{
    [Fact]
    public void ClassTypeNameIsOnlyTypeOnFinalPSObject()
    {
        var underlyingObject = new PSObject();
        underlyingObject.TypeNames.Clear();
        underlyingObject.TypeNames.Add("MyType");
        underlyingObject.TypeNames.Add("AnotherType");

        var item = new TestItem(ItemPath.Root, underlyingObject);
        var pipelineObject = item.ToPipelineObject(p => p.ToString());

        pipelineObject.TypeNames.Should().HaveCount(1);
        pipelineObject.TypeNames.Single().Should().Be(typeof(TestItem).FullName);
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
    
    [Theory]
    [InlineData("myali*", "myalias")]
    [InlineData("myalias", "myalias")]
    [InlineData("te*", "test")]
    [InlineData("test", "test")]
    [InlineData("foo", "test")]
    public void CanGetMatchingCacheableItem(string pattern, string expectedMatchingPath)
    {
        var testItem = new TestItem(ItemPath.Root, new PSObject());
        testItem.AddAlias("myalias");

        var matchingPath = testItem.MatchingCacheablePath(new ItemPath(pattern));
        matchingPath.FullName.Should().Be(expectedMatchingPath);
    }

    [Fact]
    public void PropertyWithItemPropertyAttributeAddedToPipelineObject()
    {
        var item = new TestItem(ItemPath.Root, new PSObject());
        item.CustomProperty = "test-value";
        var pipelineObject = item.ToPipelineObject(s => s.FullName);

        pipelineObject.Properties["CustomProperty"].Should().NotBeNull();
        pipelineObject.Properties["CustomProperty"].Value.Should().Be("test-value");
    }
    
    [Fact]
    public void PropertyWithItemPropertyAttributeAndCustomNameAddedToPipelineObject()
    {
        var item = new TestItem(ItemPath.Root, new PSObject());
        item.CustomPropertyWithName = "custom-test-value";
        var pipelineObject = item.ToPipelineObject(s => s.FullName);

        pipelineObject.Properties["CustomPropName"].Should().NotBeNull();
        pipelineObject.Properties["CustomPropName"].Value.Should().Be("custom-test-value");
    }
    

    public class TestItem : Item
    {
        public TestItem(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject) {}
        
        public TestItem(ItemPath parentPath, object underlyingObject) : base(parentPath, underlyingObject) {}

        public override string ItemName => "test";
        public override bool IsContainer => false;
        public override IEnumerable<string> Aliases { get; } = new List<string>();

        public void AddAlias(string alias)
        {
            ((List<string>)Aliases).Add(alias);
        }

        public new T? Property<T>(string propertyName) => base.Property<T>(propertyName);
        
        [ItemProperty]
        public string? CustomProperty { get; set; }
        
        [ItemProperty("CustomPropName")]
        public string? CustomPropertyWithName { get; set; }
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
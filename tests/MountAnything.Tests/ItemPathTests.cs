using System;
using FluentAssertions;
using Xunit;

namespace MountAnything.Tests;

public class ItemPathTests
{
    [Fact]
    public void PathIsNormalizedOnConstruction()
    {
        var path = new ItemPath("/services/ec2/instances");

        path.FullName.Should().Be("services/ec2/instances");
    }

    [Fact]
    public void BackslashesNormalizedAsForwardSlashes()
    {
        var path = new ItemPath(@"services\ec2\instances");
        path.FullName.Should().Be("services/ec2/instances");
    }

    [Fact]
    public void CanGetParent()
    {
        var path = new ItemPath("services/ec2/instances");
        path.Parent.FullName.Should().Be("services/ec2");
    }

    [Fact]
    public void CanCombineWithLeafString()
    {
        var path = new ItemPath("services/ec2");
        path.Combine("instances").FullName.Should().Be("services/ec2/instances");
    }

    [Fact]
    public void CanCombineWithAnotherItemPath()
    {
        var path = new ItemPath("services/ec2");
        var itemPath = new ItemPath("instances/i-12345");

        path.Combine(itemPath).FullName.Should().Be("services/ec2/instances/i-12345");
    }

    [Fact]
    public void EmptyStringConsideredRoot()
    {
        new ItemPath("").IsRoot.Should().BeTrue();
    }
    
    [Fact]
    public void SlashNormalizedAsEmptyString()
    {
        new ItemPath("/").FullName.Should().Be("");
    }

    [Fact]
    public void CanGetAncestorByName()
    {
        new ItemPath("services/ec2/instances/i-12345")
            .Ancestor("ec2")
            .FullName.Should().Be("services/ec2");
    }

    [Fact]
    public void AncestorThatDoesNotExistThrows()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new ItemPath("services/ec2/instances/i-12345").Ancestor("ecs");
        });
    }

    [Fact]
    public void ToStringReturnsFullName()
    {
        new ItemPath("services/ec2/instances").ToString().Should().Be("services/ec2/instances");
    }

    [Theory]
    [InlineData("services/ec2/instances", "services/ec2/*")]
    [InlineData("services/ec2/instances/i-12345", "services/ec2/instances/i-*")]
    [InlineData("services/ec2/instances/i-12345", "services/ec2/instances/i-12345*")]
    public void MatchesPattern_ReturnsTrueForMatchingPatterns(string path, string pathWithPattern)
    {
        new ItemPath(path).MatchesPattern(new ItemPath(pathWithPattern)).Should().BeTrue();
    }
    
    [Theory]
    [InlineData("services/ec2/instances", "services/ecs/*")]
    [InlineData("services/ec2/instances/i-12345", "services/ec2/instances/i-234*")]
    [InlineData("services/ec2/instances/i-12345", "services/ec2/instances/i-12346*")]
    public void MatchesPattern_ReturnsFalseForUnmatchingPatterns(string path, string pathWithPattern)
    {
        new ItemPath(path).MatchesPattern(new ItemPath(pathWithPattern)).Should().BeFalse();
    }
}
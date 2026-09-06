using RxMvvmLight.Helpers;

namespace RxMvvmLight.Tests;

public class PropertyNameHelperTests
{
    [Fact]
    public void ExtractNames_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames(null!));
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames(""));
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames("   "));
    }

    [Fact]
    public void ExtractNames_ContainsBrace_Throws()
    {
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames("x => { x.Name }"));
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames("x => x.List[0]"));
    }

    [Fact]
    public void ExtractNames_NestedParentheses_Throws()
    {
        Assert.Throws<ArgumentException>(() => PropertyNameHelper.ExtractNames("x => (x.Foo(x.Bar))"));
    }

    [Fact]
    public void ExtractNames_SimpleProperty_ReturnsSingle()
    {
        var result = PropertyNameHelper.ExtractNames("x => x.Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ParenthesizedParam_ReturnsSingle()
    {
        var result = PropertyNameHelper.ExtractNames("(x) => x.Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ClosureCapture_ReturnsSingle()
    {
        var result = PropertyNameHelper.ExtractNames("x => Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ClosureCapture_ParenthesizedParam()
    {
        var result = PropertyNameHelper.ExtractNames("(x) => Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ClosureCapture_WithSpaces()
    {
        var result = PropertyNameHelper.ExtractNames("x  =>  Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_TupleWithParamAccess_ReturnsMultiple()
    {
        var result = PropertyNameHelper.ExtractNames("x => (x.Name, x.Email)");
        Assert.Equal(new[] { "Name", "Email" }, result);
    }

    [Fact]
    public void ExtractNames_TupleWithClosureCapture_ReturnsMultiple()
    {
        var result = PropertyNameHelper.ExtractNames("x => (Name, Email)");
        Assert.Equal(new[] { "Name", "Email" }, result);
    }

    [Fact]
    public void ExtractNames_NestedProperty_TakesFirstSegment()
    {
        var result = PropertyNameHelper.ExtractNames("x => x.Foo.Bar.Baz");
        Assert.Equal(new[] { "Foo" }, result);
    }

    [Fact]
    public void ExtractNames_NullableProperty_RemovesSuffix()
    {
        var result = PropertyNameHelper.ExtractNames("x => x.Name?");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ForgivingProperty_RemovesSuffix()
    {
        var result = PropertyNameHelper.ExtractNames("x => x.Name!");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_SpaceBetweenParamAndDot_ReturnsName()
    {
        var result = PropertyNameHelper.ExtractNames("x  =>  x . Name");
        Assert.Equal(new[] { "Name" }, result);
    }

    [Fact]
    public void ExtractNames_ParenthesizedParamWithSpaces()
    {
        var result = PropertyNameHelper.ExtractNames("( x ) => x.Name");
        Assert.Equal(new[] { "Name" }, result);
    }
}

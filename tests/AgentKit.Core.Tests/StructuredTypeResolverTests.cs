using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>结构化类型解析器测试。</summary>
public class StructuredTypeResolverTests
{
    [Fact]
    public void TryResolve_KnownType_ReturnsTrue()
    {
        var resolver = new StructuredTypeResolver();
        var found = resolver.TryResolve(typeof(TestPerson).FullName!, out var type);

        Assert.True(found);
        Assert.NotNull(type);
        Assert.Equal(typeof(TestPerson), type);
    }

    [Fact]
    public void TryResolve_RegisteredType_ReturnsTrue()
    {
        var resolver = new StructuredTypeResolver();
        resolver.Register("MyType", typeof(TestPerson));

        var found = resolver.TryResolve("MyType", out var type);

        Assert.True(found);
        Assert.Equal(typeof(TestPerson), type);
    }

    [Fact]
    public void TryResolve_UnknownType_ReturnsFalse()
    {
        var resolver = new StructuredTypeResolver();
        var found = resolver.TryResolve("NonExistent.Namespace.Foo", out var type);

        Assert.False(found);
        Assert.Null(type);
    }

    [Fact]
    public void TryResolve_CachesResult()
    {
        var resolver = new StructuredTypeResolver();
        var name = typeof(TestPerson).FullName!;

        resolver.TryResolve(name, out _);
        resolver.TryResolve(name, out var type2);

        Assert.NotNull(type2);
    }

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}

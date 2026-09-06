using R3;
using RxMvvmLight;

namespace RxMvvmLight.Tests;

public class ObserveTests
{
    [Fact]
    public void ObserveChanged_SingleProperty_ReplaysCurrent_AndPushesChanges()
    {
        var vm = new TestVm();
        var received = new List<string>();
        vm.ObserveChanged(x => x.Name).Subscribe(received.Add);

        Assert.Equal([""], received);

        vm.Name = "a";
        Assert.Equal(["", "a"], received);

        vm.Name = "b";
        Assert.Equal(["", "a", "b"], received);
    }

    [Fact]
    public void ObserveChanged_MultipleProperties_ReplaysCurrent_AndPushesChanges()
    {
        var vm = new TestVm();
        var received = new List<(string Name, string Email)>();
        vm.ObserveChanged(x => (x.Name, x.Email)).Subscribe(received.Add);

        Assert.Equal([("", "")], received);

        vm.Name = "n";
        Assert.Equal([("", ""), ("n", "")], received);

        vm.Email = "e";
        Assert.Equal([("", ""), ("n", ""), ("n", "e")], received);

        vm.Name = "n2";
        Assert.Equal([("", ""), ("n", ""), ("n", "e"), ("n2", "e")], received);
    }

    [Fact]
    public void ObserveChanged_IgnoresUnrelatedProperties()
    {
        var vm = new TestVm();
        var received = new List<string>();
        vm.ObserveChanged(x => x.Name).Subscribe(received.Add);

        vm.Email = "e";
        Assert.Equal([""], received);

        vm.Name = "n";
        Assert.Equal(["", "n"], received);
    }

    [Fact]
    public void ObserveChanging_SingleProperty_PushesOldValues()
    {
        var vm = new TestVm();
        var received = new List<string>();
        vm.ObserveChanging(x => x.Name).Subscribe(received.Add);

        Assert.Empty(received);

        vm.Name = "a";
        Assert.Equal([""], received);

        vm.Name = "b";
        Assert.Equal(["", "a"], received);
    }

    [Fact]
    public void ObserveChanging_MultipleProperties_PushesOldValues()
    {
        var vm = new TestVm();
        var received = new List<(string Name, string Email)>();
        vm.ObserveChanging(x => (x.Name, x.Email)).Subscribe(received.Add);

        Assert.Empty(received);

        vm.Name = "n";
        Assert.Equal([("", "")], received);

        vm.Email = "e";
        Assert.Equal([("", ""), ("n", "")], received);

        vm.Name = "n2";
        Assert.Equal([("", ""), ("n", ""), ("n", "e")], received);
    }

    [Fact]
    public void ObserveChanging_IgnoresUnrelatedProperties()
    {
        var vm = new TestVm();
        var received = new List<string>();
        vm.ObserveChanging(x => x.Name).Subscribe(received.Add);

        vm.Email = "e";
        Assert.Empty(received);

        vm.Name = "n";
        Assert.Equal([""], received);
    }
}

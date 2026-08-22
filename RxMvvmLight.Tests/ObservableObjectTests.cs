using R3;
using RxMvvmLight;

namespace RxMvvmLight.Tests;

public class ObservableObjectTests
{
    [Fact]
    public void GetObservable_ReplaysCurrent_AndPushesChanges()
    {
        var vm = new TestVm();
        var received = new List<string?>();
        vm.GetObservable(vm.Name).Subscribe(received.Add);

        Assert.Equal(new string?[] { "" }, received);

        vm.Name = "a";
        Assert.Equal(new string?[] { "", "a" }, received);

        vm.Name = "b";
        Assert.Equal(new string?[] { "", "a", "b" }, received);
    }

    [Fact]
    public void GetObservable_MultipleValues_CombinesLatest()
    {
        var vm = new TestVm();
        var received = new List<(string, string)>();
        vm.GetObservable(vm.Name, vm.Email).Subscribe(received.Add);

        vm.Name = "n";
        vm.Email = "e";
        vm.Name = "n2";

        Assert.Equal(new[] { ("", ""), ("n", ""), ("n", "e"), ("n2", "e") }, received);
    }

    [Fact]
    public void ObserveChanged_FollowsNestedChain()
    {
        var vm = new TestVm();
        var received = new List<string?>();
        vm.ObserveChanged(x => x.Foo.Title).Subscribe(received.Add);

        Assert.Equal(new string?[] { "" }, received);

        vm.Foo.Title = "t1";
        Assert.Equal(new string?[] { "", "t1" }, received);

        vm.Foo.Title = "t2";
        Assert.Equal(new string?[] { "", "t1", "t2" }, received);
    }

    [Fact]
    public void ObserveChanged_SwitchesWhenBranchReassigned()
    {
        var vm = new TestVm();
        var received = new List<string?>();
        vm.ObserveChanged(x => x.Foo.Title).Subscribe(received.Add);

        vm.Foo = new SubVm { Title = "new" };
        Assert.Equal(new string?[] { "", "new" }, received);

        vm.Foo.Title = "t1";
        Assert.Equal(new string?[] { "", "new", "t1" }, received);
    }
}
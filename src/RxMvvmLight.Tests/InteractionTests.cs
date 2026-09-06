using RxMvvmLight;

namespace RxMvvmLight.Tests;

public class InteractionTests
{
    [Fact]
    public async Task Handle_WithSyncHandler_ReturnsOutput()
    {
        var interaction = new Interaction<string, int>();
        interaction.RegisterHandler(input => input.Length);

        var result = await interaction.Handle("hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task Handle_WithAsyncHandler_ReturnsOutput()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(async input =>
        {
            await Task.Delay(10);
            return "answered: " + input;
        });

        var result = await interaction.Handle("question");

        Assert.Equal("answered: question", result);
    }

    [Fact]
    public async Task Handle_SequentialRequests_EachGetsOwnResult()
    {
        var interaction = new Interaction<string, int>();
        interaction.RegisterHandler(input => input.Length);

        var a = await interaction.Handle("aa");
        var b = await interaction.Handle("bbbb");

        Assert.Equal(2, a);
        Assert.Equal(4, b);
    }

    [Fact]
    public async Task Handle_NoHandler_Throws()
    {
        var interaction = new Interaction<string, string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
    }

    [Fact]
    public async Task Handle_AfterHandlerUnregistered_Throws()
    {
        var interaction = new Interaction<string, string>();
        var registration = interaction.RegisterHandler(input => input);
        registration.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
    }

    [Fact]
    public async Task Handle_HandlerException_Propagates()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler((Func<string, string>)(input => throw new InvalidOperationException("boom")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Handle_MultipleHandlers_OnlyTopOfStackIsInvoked()
    {
        var interaction = new Interaction<string, string>();
        var invoked = new List<string>();
        interaction.RegisterHandler(input => { invoked.Add("h1"); return "first"; });
        interaction.RegisterHandler(input => { invoked.Add("h2"); return "second"; });

        var result = await interaction.Handle("x");

        Assert.Equal(new[] { "h2" }, invoked);
        Assert.Equal("second", result);
    }

    [Fact]
    public async Task Handle_UnregisteringTopRestoresPreviousHandler()
    {
        var interaction = new Interaction<string, string>();
        var registration = interaction.RegisterHandler(input => "first");
        var top = interaction.RegisterHandler(input => "second");

        Assert.Equal("second", await interaction.Handle("x"));

        top.Dispose();

        Assert.Equal("first", await interaction.Handle("x"));

        registration.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
    }

    [Fact]
    public async Task Dispose_ThenHandle_ThrowsObjectDisposed()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => input);
        interaction.Dispose();
        interaction.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => interaction.Handle("x"));
    }
}
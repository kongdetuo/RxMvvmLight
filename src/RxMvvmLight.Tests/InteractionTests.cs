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

    [Fact]
    public async Task Handle_WithPreviousHandler_CallsPreviousHandler()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => "first");
        interaction.RegisterHandler(async (input, previous) =>
        {
            var result = await previous(input);
            return result + " -> second";
        });

        var result = await interaction.Handle("x");

        Assert.Equal("first -> second", result);
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_CanModifyInput()
    {
        var interaction = new Interaction<string, int>();
        interaction.RegisterHandler(input => input.Length);
        interaction.RegisterHandler(async (input, previous) =>
        {
            var modified = input.ToUpper();
            return await previous(modified);
        });

        var result = await interaction.Handle("hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_CanSkipPreviousHandler()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => "first");
        interaction.RegisterHandler(async (input, previous) =>
        {
            return "override";
        });

        var result = await interaction.Handle("x");

        Assert.Equal("override", result);
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_NoPreviousHandler_Throws()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(async (input, previous) =>
        {
            return await previous(input);
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
        Assert.Equal("没有回退 handler", ex.Message);
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_MultipleChains_ExecutesInOrder()
    {
        var interaction = new Interaction<string, string>();
        var order = new List<string>();
        interaction.RegisterHandler(input =>
        {
            order.Add("first");
            return "first";
        });
        interaction.RegisterHandler(async (input, previous) =>
        {
            order.Add("second");
            var result = await previous(input);
            return result + " -> second";
        });
        interaction.RegisterHandler(async (input, previous) =>
        {
            order.Add("third");
            var result = await previous(input);
            return result + " -> third";
        });

        var result = await interaction.Handle("x");

        Assert.Equal(new[] { "third", "second", "first" }, order);
        Assert.Equal("first -> second -> third", result);
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_UnregisterRestoresPrevious()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => "first");
        var middleware = interaction.RegisterHandler(async (input, previous) =>
        {
            var result = await previous(input);
            return result + " -> middleware";
        });

        Assert.Equal("first -> middleware", await interaction.Handle("x"));

        middleware.Dispose();

        Assert.Equal("first", await interaction.Handle("x"));
    }

    [Fact]
    public async Task Handle_WithPreviousHandler_ExceptionInMiddleware_Propagates()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => "first");
        interaction.RegisterHandler(async (input, previous) =>
        {
            throw new InvalidOperationException("middleware error");
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => interaction.Handle("x"));
        Assert.Equal("middleware error", ex.Message);
    }

    [Fact]
    public void RegisterHandler_AfterDispose_ThrowsObjectDisposed()
    {
        var interaction = new Interaction<string, string>();
        interaction.RegisterHandler(input => input);
        interaction.Dispose();

        Assert.Throws<ObjectDisposedException>(() => interaction.RegisterHandler(input => input));
    }
}
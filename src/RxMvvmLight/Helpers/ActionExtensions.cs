namespace RxMvvmLight.Helpers
{
    internal static class ActionExtensions
    {
        public static Func<ValueTask> AsValueTask(this Action action)
        {
            return () =>
            {
                action();
                return ValueTask.CompletedTask;
            };
        }

        public static Func<T, ValueTask> AsValueTask<T>(this Action<T> action)
        {
            return v =>
            {
                action(v);
                return ValueTask.CompletedTask;
            };
        }
    }
}

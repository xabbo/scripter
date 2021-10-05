using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Xabbo.Scripter.Services
{
    public class WpfContext : IUiContext
    {
        public Dispatcher Dispatcher { get; }

        public WpfContext(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        public bool IsSynchronized => Dispatcher.CheckAccess();

        public void Invoke(Action callback) => Dispatcher.Invoke(callback);
        public TResult Invoke<TResult>(Func<TResult> callback) => Dispatcher.Invoke(callback);
        public Task InvokeAsync(Action callback) => Dispatcher.InvokeAsync(callback).Task;
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback) => Dispatcher.InvokeAsync(callback).Task;
    }
}

using System;
using System.Threading.Tasks;

namespace b7.Scripter.Services
{
    public interface IUIContext
    {
        bool IsSynchronized { get; }
        void Invoke(Action callback);
        Task InvokeAsync(Action callback);
        TResult Invoke<TResult>(Func<TResult> callback);
        Task<TResult> InvokeAsync<TResult>(Func<TResult> callback);
    }
}

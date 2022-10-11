using System;

using Xabbo.Messages;
using Xabbo.Messages.Dispatcher;

namespace Xabbo.Scripter.Scripting;

internal class Intercept : IDisposable
{
    private bool _disposed;

    public IMessageDispatcher Dispatcher { get; }
    public Header Header { get; }
    public Action<InterceptArgs> Callback { get; }

    public Intercept(IMessageDispatcher dispatcher, Header header, Action<InterceptArgs> callback)
    {
        Dispatcher = dispatcher;
        Header = header;
        Callback = callback;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Dispatcher.RemoveIntercept(Header, Callback);
    }
}

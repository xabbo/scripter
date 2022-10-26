using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Xabbo.Messages;
using Xabbo.Connection;
using Xabbo.Interceptor;
using Xabbo.Interceptor.Tasks;

namespace Xabbo.Scripter.Scripting;

/*
 * Defines methods related to sending, receiving and intercepting messages.
 */
public partial class G
{
    /// <inheritdoc cref="IConnection.Send(IReadOnlyPacket)" />
    /// <param name="packet">The packet to send.</param>
    public void Send(IReadOnlyPacket packet) => Interceptor.Send(packet);

    /// <inheritdoc cref="InterceptorExtensions.Send(IInterceptor, Header)" />
    public void Send(Header header) => Interceptor.Send(header);

    /// <inheritdoc cref="IConnection.SendAsync(IReadOnlyPacket)" />
    /// <param name="packet">The packet to send.</param>
    public ValueTask SendAsync(IReadOnlyPacket packet) => Interceptor.SendAsync(packet);

    /// <inheritdoc cref="InterceptorExtensions.SendAsync(IInterceptor, Header)" />
    public ValueTask SendAsync(Header header) => Interceptor.SendAsync(header);

    /// <summary>
    /// Captures a packet with a header that matches any of the specified headers.
    /// </summary>
    /// <param name="headers">The message headers to listen for.</param>
    /// <param name="timeout">The time to wait for a packet to be captured.</param>
    /// <param name="block">Whether to block the captured packet.</param>
    /// <returns>The first packet captured with a header that matches one of the specified headers.</returns>
    public IReadOnlyPacket Receive(HeaderSet headers, int timeout = -1, bool block = false)
        => ReceiveAsync(headers, timeout, block).GetAwaiter().GetResult();

    /// <summary>
    /// Captures a packet with a header that matches any of the specified headers.
    /// </summary>
    /// <param name="tuple">The message headers to listen for.</param>
    /// <param name="timeout">The time to wait for a packet to be captured.</param>
    /// <param name="block">Whether to block the captured packet.</param>
    /// <returns>The first packet captured with a header that matches one of the specified headers.</returns>
    public IReadOnlyPacket Receive(ITuple tuple, int timeout = -1, bool block = false)
        => Receive(HeaderSet.FromTuple(tuple), timeout, block);

    /// <summary>
    /// Asynchronously captures a packet with a header that matches any of specified headers.
    /// </summary>
    /// <param name="headers">The message headers to listen for.</param>
    /// <param name="timeout">The time to wait for a packet to be captured.</param>
    /// <param name="block">Whether to block the captured packet.</param>
    /// <returns>The first packet with a header that matches one of the specified headers.</returns>
    public Task<IPacket> ReceiveAsync(HeaderSet headers, int timeout = -1, bool block = false)
    {
        return new CaptureMessageTask(Interceptor, headers, block)
            .ExecuteAsync(timeout, Ct);
    }

    /// <inheritdoc cref="ReceiveAsync(HeaderSet, int, bool)" />
    public Task<IPacket> ReceiveAsync(ITuple headers, int timeout = -1, bool block = false)
        => ReceiveAsync(HeaderSet.FromTuple(headers), timeout, block);

    /// <summary>
    /// Attempts to capture a packet with a header that matches any of the specified headers.
    /// </summary>
    /// <param name="headers">The message headers to listen for.</param>
    /// <param name="timeout">The time to wait for a packet to be captured.</param>
    /// <param name="packet">The packet that was captured.</param>
    /// <param name="block">Whether to block the captured packet.</param>
    /// <returns>True if a packet was successfully captured, or false if the operation timed out.</returns>
    public bool TryReceive(HeaderSet headers, out IReadOnlyPacket? packet, int timeout = -1, bool block = false)
    {
        packet = null;
        try
        {
            packet = Receive(headers, timeout, block);
            return true;
        }
        catch (OperationCanceledException)
        when (!Ct.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a callback to be invoked when a packet with the specified header is intercepted.
    /// </summary>
    public void OnIntercept(Header header, Action<InterceptArgs> callback)
    {
        lock (_intercepts)
        {
            _dispatcher.AddIntercept(header, callback, CurrentClient);
            _intercepts.Add(new Intercept(Interceptor.Dispatcher, header, callback));
        }
    }

    /// <summary>
    /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
    /// </summary>
    public void OnIntercept(ITuple headers, Action<InterceptArgs> callback) => OnIntercept(HeaderSet.FromTuple(headers), callback);

    /// <summary>
    /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
    /// </summary>
    public void OnIntercept(HeaderSet headers, Action<InterceptArgs> callback)
    {
        foreach (Header header in headers)
        {
            OnIntercept(header, callback);
        }
    }

    /// <summary>
    /// Registers a callback to be invoked when a packet with the specified header is intercepted.
    /// </summary>
    public void OnIntercept(Header header, Func<InterceptArgs, Task> callback)
        => OnIntercept(header, e => { callback(e); });

    /// <summary>
    /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
    /// </summary>
    public void OnIntercept(ITuple headers, Func<InterceptArgs, Task> callback)
        => OnIntercept(HeaderSet.FromTuple(headers), callback);

    /// <summary>
    /// Registers a callback to be invoked when a packet with any of the specified headers is intercepted.
    /// </summary>
    public void OnIntercept(HeaderSet headers, Func<InterceptArgs, Task> callback)
       => OnIntercept(headers, e => { callback(e); });
}

using System;
using System.Reflection;

namespace Xabbo.Scripter.Scripting
{
    internal class Unsubscriber : IDisposable
    {
        private readonly object _eventSource;
        private readonly EventInfo _eventInfo;
        private readonly Delegate _handler;

        private bool _disposed;

        public Unsubscriber(object eventSource, EventInfo eventInfo, Delegate handler)
        {
            _eventSource = eventSource;
            _eventInfo = eventInfo;
            _handler = handler;
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _eventInfo.RemoveEventHandler(_eventSource, _handler);
            }
        }
    }
}

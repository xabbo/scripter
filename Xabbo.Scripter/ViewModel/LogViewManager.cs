using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.ViewModel
{
    public class LogViewManager : ObservableObject
    {
        public ObservableLoggerProvider Logger { get; }

        public LogViewManager(IEnumerable<ILoggerProvider> loggers)
        {
            Logger = loggers.OfType<ObservableLoggerProvider>().First();
        }
    }
}

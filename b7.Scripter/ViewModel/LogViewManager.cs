using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using GalaSoft.MvvmLight;

using b7.Scripter.Services;

namespace b7.Scripter.ViewModel
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

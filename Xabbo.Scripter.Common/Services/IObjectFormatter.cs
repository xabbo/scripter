using System;

namespace Xabbo.Scripter.Services;

public interface IObjectFormatter
{
    string FormatObject(object? obj);
    string FormatException(Exception ex);
}

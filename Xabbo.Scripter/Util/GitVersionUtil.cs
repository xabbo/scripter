using System;
using System.Reflection;

namespace Xabbo.Scripter.Util;

internal static class GitVersionUtil
{
    public static string GetSemanticVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetType("GitVersionInformation")
            ?.GetField("SemVer")
            ?.GetValue(null) as string
            ?? throw new InvalidOperationException($"Failed to get SemVer from GitVersionInformation.");
    }
}

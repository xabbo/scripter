using System;
using System.Reflection;

namespace Xabbo.Scripter.Util;

internal static class GitVersionUtil
{
    public static string? GetSemanticVersion(Assembly assembly)
    {
        return assembly
            .GetType("GitVersionInformation")
            ?.GetField("FullSemVer")
            ?.GetValue(null) as string;
    }
}

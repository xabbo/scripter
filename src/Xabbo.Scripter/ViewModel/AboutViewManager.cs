using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using Xabbo.Scripter.Util;

namespace Xabbo.Scripter.ViewModel;

public class AboutViewManager : ObservableObject
{
    private string _scripterVersion = string.Empty;
    public string ScripterVersion
    {
        get => _scripterVersion;
        set => Set(ref _scripterVersion, value);
    }

    private string _xabboCommonVersion = string.Empty;
    public string XabboCommonVersion
    {
        get => _xabboCommonVersion;
        set => Set(ref _xabboCommonVersion, value);
    }

    private string _xabboGEarthVersion = string.Empty;
    public string XabboGEarthVersion
    {
        get => _xabboGEarthVersion;
        set => Set(ref _xabboGEarthVersion, value);
    }

    private string _xabboCoreVersion = string.Empty;
    public string XabboCoreVersion
    {
        get => _xabboCoreVersion;
        set => Set(ref _xabboCoreVersion, value);
    }

    public AboutViewManager()
    {
        ScripterVersion = GetAssemblyVersion("Xabbo.Scripter");
        XabboCommonVersion = GetAssemblyVersion("Xabbo.Common");
        XabboGEarthVersion = GetAssemblyVersion("Xabbo.GEarth");
        XabboCoreVersion = GetAssemblyVersion("Xabbo.Core");
    }

    private static string GetAssemblyVersion(string assemblyName)
    {
        string? version = GitVersionUtil.GetSemanticVersion(Assembly.Load(assemblyName));

        if (version is null)
        {
            var asm = Assembly.Load(assemblyName);
            var attr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attr is not null)
                version = $"v{attr.InformationalVersion}";
            else
                version = "v" + (Assembly.Load(assemblyName).GetName().Version?.ToString(3) ?? " unknown");
        }

        return version;
    }
}

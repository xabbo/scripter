using System;
using System.Windows;

using Dragablz;

using Xabbo.Scripter.View;
using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.Tabs;

public class ScripterInterTabClient : IInterTabClient
{
    private readonly ScriptsViewManager _viewManager;

    public ScripterInterTabClient(ScriptsViewManager viewManager)
    {
        _viewManager = viewManager;
    }

    public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
    {
        ScriptWindow window = new ScriptWindow() { DataContext = _viewManager };

        // return new NewTabHost<ScriptWindow>(window, window.TabablzControl);
        return null!;
    }

    public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
    {
        return TabEmptiedResponse.CloseWindowOrLayoutBranch;
    }
}

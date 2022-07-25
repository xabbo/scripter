using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Xabbo.Scripter.View.Pages;

public partial class DebugPage : Page
{
    public DebugPage()
    {
        InitializeComponent();
    }

    private void ButtonAttachDebugger_Click(object sender, RoutedEventArgs e)
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }
}

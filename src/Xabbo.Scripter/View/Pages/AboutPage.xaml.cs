using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View.Pages;

/// <summary>
/// Interaction logic for AboutPage.xaml
/// </summary>
public partial class AboutPage : Page
{
    public AboutPage(AboutViewManager manager)
    {
        DataContext = manager;

        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object? sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.ToString(),
            UseShellExecute = true
        });
    }
}

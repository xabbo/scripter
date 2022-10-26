using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View;

public partial class MainWindow : Window, INavigationWindow
{
    private readonly INavigationService _nav;

    public MainWindow(MainViewManager manager,
        INavigationService nav,
        IPageService pageService)
    {
        _nav = nav;
        DataContext = manager;

        InitializeComponent();

        _nav.SetNavigation(RootNavigation);
        SetPageService(pageService);

        Loaded += MainWindow_Loaded;
        Activated += MainWindow_Activated;

        RootFrame.Navigating += RootFrame_Navigating;
    }

    private void RootFrame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Refresh)
            e.Cancel = true;
    }

    private void ButtonPin_Click(object sender, RoutedEventArgs e) => Topmost = !Topmost;

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        MainViewManager mainViewManager = (MainViewManager)DataContext;
        await Task.Run(() => mainViewManager.InitializeAsync(CancellationToken.None));
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        Activated -= MainWindow_Activated;

        Navigate(typeof(Pages.LogPage));
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Application.Current.Shutdown();
    }

    #region - INavigationWindow -
    public void CloseWindow() => Close();

    public Frame GetFrame() => RootFrame;

    public INavigation GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

    public void ShowWindow() => Show();
    #endregion
}

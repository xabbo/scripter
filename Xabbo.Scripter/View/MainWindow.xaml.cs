using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using MaterialDesignExtensions.Controls;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View
{
    public partial class MainWindow : MaterialWindow
    {
        public MainWindow(MainViewManager manager)
        {
            DataContext = manager;

            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            MainViewManager mainViewManager = (MainViewManager)DataContext;
            await Task.Run(() => mainViewManager.InitializeAsync(CancellationToken.None));
        }
    }
}

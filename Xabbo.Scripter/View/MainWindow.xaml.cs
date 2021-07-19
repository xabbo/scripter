using System;
using System.Threading;
using System.Windows;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View
{
    public partial class MainWindow : Window
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

            await ((MainViewManager)DataContext).InitializeAsync(CancellationToken.None);
        }
    }
}

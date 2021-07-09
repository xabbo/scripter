using System;
using System.Threading;
using System.Windows;

using b7.Scripter.ViewModel;

namespace b7.Scripter.View
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

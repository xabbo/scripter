using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View
{
    public partial class ScriptListView : UserControl
    {
        public ScriptsViewManager Manager => (ScriptsViewManager)DataContext;

        public ScriptListView()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not ScriptViewModel script)
            {
                return;
            }

            Manager.SelectScript(script);
        }

        private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dataGrid) return;

            Manager.SelectedItems = dataGrid.SelectedItems;
        }
    }
}

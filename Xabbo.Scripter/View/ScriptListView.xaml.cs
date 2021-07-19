using Xabbo.Scripter.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            if (sender is not ListViewItem item ||
                item.DataContext is not ScriptViewModel script)
            {
                return;
            }

            if (!script.IsOpen)
            {
                script.IsOpen = true;
                Manager.OpenScripts.Refresh();
            }

            Manager.SelectedItem = script;
        }
    }
}

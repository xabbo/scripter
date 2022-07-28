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

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.View.Pages
{
    /// <summary>
    /// Interaction logic for ScriptsPage.xaml
    /// </summary>
    public partial class ScriptsPage : Page
    {
        public ScriptsViewManager Manager { get; }

        public ScriptsPage(ScriptsViewManager manager)
        {
            Manager = manager;
            DataContext = manager;

            InitializeComponent();
        }

        private void DragablzItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not ScriptViewModel scriptViewModel)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Middle)
            {
                e.Handled = true;

                Manager.CloseScript(scriptViewModel);
            }
        }
    }
}

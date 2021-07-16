using System;
using System.Windows.Controls;
using System.Windows.Input;

using b7.Scripter.ViewModel;

namespace b7.Scripter.View
{
    public partial class ScriptsView : UserControl
    {
        public ScriptsViewManager Manager => (ScriptsViewManager)DataContext;

        public ScriptsView()
        {
            InitializeComponent();
        }

        private void ScriptTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TabItem tabItem ||
                tabItem.DataContext is not ScriptViewModel scriptViewModel)
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
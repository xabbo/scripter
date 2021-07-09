using b7.Scripter.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace b7.Scripter.View
{
    public partial class ScriptsView : UserControl
    {
        public ScriptsViewManager Manager => (ScriptsViewManager)DataContext;

        public ScriptsView()
        {
            InitializeComponent();
        }

        #region - Events -
        private void ScriptManagerView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= ScriptManagerView_Loaded;

            // TODO load all script files here

            new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Background,
                UpdateViewModels,
                Dispatcher
            ).Start();
        }
        #endregion

        #region - Logic -
        private void UpdateViewModels(object? sender, EventArgs e)
        {
            //foreach (var script in ViewModel.Scripts)
                //script.UpdateRuntime();
        }
        #endregion

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            return;
            /*
            if (tabControlScripts.SelectedIndex == tabControlScripts.Items.Count - 1)
            {
                var script = new ScriptViewModel();
                var scriptView = new ScriptView(script);

                var names = new HashSet<string>(scriptItems.Select(x => x.Name));
                
                int i = 0;
                do { script.Name = $"new {++i}"; }
                while (names.Contains(script.Name));

                var scriptTabItem = new TabItem() { Content = scriptView, DataContext = script };
                scriptTabItem.MouseDown += ScriptTabItem_MouseDown;
                BindingOperations.SetBinding(scriptTabItem, TabItem.HeaderProperty, new Binding("Header"));

                scriptItems.Add(script);
                scriptManager.Scripts.Add(script);

                tabControlScripts.Items.Insert(tabControlScripts.Items.Count - 1, scriptTabItem);
                tabControlScripts.SelectedItem = scriptTabItem;
            }
            */

            if (tabControl.SelectedItem == null/*tabItemNew*/)
            {
                ScriptViewModel scriptViewModel = new ScriptViewModel(Manager.Engine, new Model.ScriptModel());
                ScriptView scriptView = new ScriptView() { DataContext = scriptViewModel };

                var names = new HashSet<string>(
                    Manager.Scripts.Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase
                );

                int i = 0;
                do { scriptViewModel.Name = $"new {++i}"; }
                while (names.Contains(scriptViewModel.Name));
                
                TabItem scriptTab = new TabItem() { Content = scriptView, DataContext = scriptViewModel };
                scriptTab.MouseDown += ScriptTab_MouseDown;
                BindingOperations.SetBinding(scriptTab, TabItem.HeaderProperty, new Binding("Header"));

                Manager.Scripts.Add(scriptViewModel);

                tabControl.Items.Insert(
                    tabControl.Items.Count - 1,
                    scriptTab
                );
                tabControl.SelectedItem = scriptTab;
            }
        }

        private void ScriptTab_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not TabItem tabItem ||
                tabItem.DataContext is not ScriptViewModel scriptViewModel)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Middle)
            {
                e.Handled = true;

                if (tabControl.SelectedItem == tabItem)
                    tabControl.SelectedIndex--;

                tabControl.Items.Remove(tabItem);
                // ((ScriptView)tabItem.Content).Dispose();
            }
        }
    }
}
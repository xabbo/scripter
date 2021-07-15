using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;

using GalaSoft.MvvmLight;

using b7.Scripter.Model;
using b7.Scripter.Services;
using b7.Scripter.Engine;

namespace b7.Scripter.ViewModel
{
    public class ScriptsViewManager : ObservableObject
    {
        private readonly IUIContext _uiContext;
        private readonly ScriptEngine _engine;

        public ScriptEngine Engine => _engine;

        private readonly object _newScriptTab = new { Header = "+" };
        private readonly ObservableCollection<ScriptViewModel> _scripts;
        private readonly CompositeCollection _tabCollection;

        private int _currentScriptIndex = 1;

        public ICollectionView Tabs { get; }
        public ICollectionView Scripts { get; }
        public ICollectionView OpenScripts { get; }

        public string Header { get; } = "*";

        private object? selectedItem;
        public object? SelectedItem
        {
            get => selectedItem;
            set => Set(ref selectedItem, value);
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get => selectedIndex;
            set => Set(ref selectedIndex, value);
        }

        public ScriptsViewManager(IUIContext uiContext, ScriptEngine engine)
        {
            _uiContext = uiContext;
            _engine = engine;

            _scripts = new ObservableCollection<ScriptViewModel>();

            Scripts = CollectionViewSource.GetDefaultView(_scripts);
            OpenScripts = new CollectionViewSource { Source = _scripts }.View;
            OpenScripts.Filter = obj =>
            {
                return
                    obj is ScriptViewModel scriptViewModel &&
                    scriptViewModel.IsOpen;
            };

            _tabCollection = new CompositeCollection()
            {
                new CollectionContainer { Collection = new object[] { this } },
                new CollectionContainer { Collection = OpenScripts },
                new CollectionContainer { Collection = new object[] { _newScriptTab } }
            };

            Tabs = CollectionViewSource.GetDefaultView(_tabCollection);

            if (uiContext is WpfContext wpfContext)
            {
                DispatcherTimer timer = new(
                    TimeSpan.FromSeconds(0.1),
                    DispatcherPriority.Background,
                    Timer_Tick,
                    wpfContext.Dispatcher
                );
                timer.Start();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (ScriptViewModel viewModel in _scripts)
            {
                viewModel.UpdateRuntime();
            }
        }

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);

            if (propertyName == nameof(SelectedItem))
            {
                if (SelectedItem == _newScriptTab)
                {
                    string scriptName = $"script {_currentScriptIndex++}";
                    ScriptViewModel scriptViewModel = new(_engine, new ScriptModel { Name = scriptName });
                    _scripts.Add(scriptViewModel);

                    _uiContext.InvokeAsync(() => SelectedItem = scriptViewModel);
                }
            }
        }
    }
}

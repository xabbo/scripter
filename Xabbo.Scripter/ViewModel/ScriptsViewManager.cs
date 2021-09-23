using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using Xabbo.Scripter.Model;
using Xabbo.Scripter.Services;
using Xabbo.Scripter.Engine;
using Dragablz;
using Xabbo.Scripter.Tabs;

namespace Xabbo.Scripter.ViewModel
{
    public class ScriptsViewManager : ObservableObject
    {
        private readonly IUIContext _uiContext;
        private readonly ScriptEngine _engine;

        private int _currentScriptIndex = 0;

        private readonly ObservableCollection<ScriptViewModel> _scripts;

        public ScriptEngine Engine => _engine;
        public ICollectionView Scripts { get; }
        public ObservableCollection<ScriptViewModel> OpenTabs { get; } = new();

        private object? selectedItem;
        public object? SelectedItem
        {
            get => selectedItem;
            set => Set(ref selectedItem, value);
        }

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set => Set(ref selectedIndex, value);
        }

        public ICommand OpenScriptListCommand { get; }
        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }

        public IInterTabClient InterTabClient { get; }
        public Func<object> NewItemFactory { get; }

        public ScriptsViewManager(IUIContext uiContext, ScriptEngine engine)
        {
            InterTabClient = new ScripterInterTabClient(this);

            _uiContext = uiContext;
            _engine = engine;

            _scripts = new ObservableCollection<ScriptViewModel>();
            Scripts = CollectionViewSource.GetDefaultView(_scripts);

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

            OpenScriptListCommand = new RelayCommand(OpenScriptList);
            NewTabCommand = new RelayCommand(AddNewScript);
            CloseTabCommand = new RelayCommand(CloseCurrentScript);

            LoadScripts();

            NewItemFactory = () => CreateNewScript();
        }

        private void OpenScriptList()
        {
            SelectedIndex = -1;
        }

        private void LoadScripts()
        {
            DirectoryInfo directory = Directory.CreateDirectory("scripts");

            foreach (FileInfo file in directory.EnumerateFiles("*.csx", SearchOption.TopDirectoryOnly))
            {
                ScriptModel model = new ScriptModel
                {
                    FileName = file.Name,
                    Name = Path.GetFileNameWithoutExtension(file.Name)
                };

                foreach (string line in File.ReadLines(file.FullName))
                {
                    Match match = ScriptEngine.NameRegex.Match(line);
                    if (match.Success)
                    {
                        model.Name = match.Groups["name"].Value;
                        break;
                    }
                }

                _scripts.Add(new ScriptViewModel(Engine, model)
                {
                    IsSavedToDisk = true
                });
            }
        }

        public ScriptViewModel CreateNewScript()
        {
            ScriptModel model = new ScriptModel
            {
                FileName = $"script-{++_currentScriptIndex}.csx"
            };

            ScriptViewModel scriptViewModel = new(_engine, model) { IsLoaded = true };

            _scripts.Add(scriptViewModel);

            return scriptViewModel;
        }

        public void AddNewScript()
        {
            ScriptModel model = new ScriptModel
            {
                FileName = $"script-{++_currentScriptIndex}.csx"
            };

            ScriptViewModel scriptViewModel = new(_engine, model) { IsLoaded = true };

            _scripts.Add(scriptViewModel);

            _uiContext.InvokeAsync(() => SelectedItem = scriptViewModel);
        }

        public void SelectScript(ScriptViewModel script)
        {
            if (!OpenTabs.Contains(script))
            {
                if (!script.IsLoaded)
                {
                    script.Load();
                }

                OpenTabs.Add(script);
            }

            SelectedItem = script;
        }

        public void DeleteScript(ScriptViewModel script)
        {
            _scripts.Remove(script);

            OpenTabs.Remove(script);
        }

        public void CloseScript(ScriptViewModel script)
        {
            if (SelectedIndex != -1)
            {
                if (SelectedItem == script)
                {
                    if (SelectedIndex == (OpenTabs.Count - 1))
                    {
                        SelectedIndex--;
                    }
                    else
                    {
                        SelectedIndex++;
                    }
                }
            }

            OpenTabs.Remove(script);

            if (!script.IsModified &&
                string.IsNullOrWhiteSpace(script.Code) &&
                !script.IsSavedToDisk)
            {
                DeleteScript(script);
            }
        }

        public void CloseCurrentScript()
        {
            if (SelectedItem is ScriptViewModel scriptViewModel)
            {
                CloseScript(scriptViewModel);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (ScriptViewModel viewModel in _scripts)
            {
                viewModel.UpdateRuntime();
            }
        }
    }
}

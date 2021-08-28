﻿using System;
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

namespace Xabbo.Scripter.ViewModel
{
    public class ScriptsViewManager : ObservableObject
    {
        private readonly IUIContext _uiContext;
        private readonly ScriptEngine _engine;

        public ScriptEngine Engine => _engine;

        private readonly object _newScriptTab = new { Header = "+" };
        private readonly ObservableCollection<ScriptViewModel> _scripts;
        private readonly CompositeCollection _tabCollection;

        private int _currentScriptIndex = 0;

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

        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }

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

            NewTabCommand = new RelayCommand(AddNewScript);
            CloseTabCommand = new RelayCommand(CloseCurrentScript);

            LoadScripts();
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
                    IsOpen = false,
                    IsSavedToDisk = true
                });
            }
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
            if (!script.IsOpen)
            {
                if (!script.IsLoaded)
                {
                    script.Load();
                }

                script.IsOpen = true;
                OpenScripts.Refresh();
            }

            SelectedItem = script;
        }

        public void DeleteScript(ScriptViewModel script)
        {
            _scripts.Remove(script);
        }

        public void CloseScript(ScriptViewModel script)
        {
            if (SelectedItem == script)
            {
                if (SelectedIndex == _scripts.Count(x => x.IsOpen))
                    SelectedIndex--;
                else
                    SelectedIndex++;
            }

            script.IsOpen = false;

            if (!script.IsModified &&
                string.IsNullOrWhiteSpace(script.Code) &&
                !script.IsSavedToDisk)
            {
                DeleteScript(script);
            }

            OpenScripts.Refresh();
            Tabs.Refresh();
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

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);

            if (propertyName == nameof(SelectedItem))
            {
                if (SelectedItem == _newScriptTab)
                {
                    AddNewScript();
                }
            }
        }
    }
}

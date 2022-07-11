using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Input;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using MaterialDesignThemes.Wpf;

using Dragablz;

using Humanizer;

using Xabbo.Scripter.Model;
using Xabbo.Scripter.Services;
using Xabbo.Scripter.Engine;
using Xabbo.Scripter.Tabs;

namespace Xabbo.Scripter.ViewModel
{
    public class ScriptsViewManager : ObservableObject
    {
        private readonly IUiContext _uiContext;
        private readonly ScriptEngine _engine;
        private readonly FileSystemWatcher _fsw;
        private readonly ISnackbarMessageQueue _snackbar;

        private int _currentScriptIndex = 0;

        private readonly ObservableCollection<ScriptViewModel> _scripts;

        private readonly Subject<FileSystemEventArgs> _fileUpdate = new();

        private readonly Dictionary<string, ScriptGroupViewModel> _groups = new();

        public ScriptEngine Engine => _engine;
        public ICollectionView Scripts { get; }
        public ObservableCollection<ScriptViewModel> OpenTabs { get; } = new();

        private object? _selectedTabItem;
        public object? SelectedTabItem
        {
            get => _selectedTabItem;
            set => Set(ref _selectedTabItem, value);
        }

        private int _selectedTabIndex = -1;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (Set(ref _selectedTabIndex, value))
                {
                    if (value == -1)
                    {
                        Scripts.Refresh();
                    }
                }
            }
        }

        public IList SelectedItems { get; set; } = new List<ScriptViewModel>();

        private double fontSize = 12;
        public double FontSize
        {
            get => fontSize;
            set => Set(ref fontSize, value);
        }

        private GridLength _logHeight = new GridLength(60);
        public GridLength LogHeight
        {
            get => _logHeight;
            set => Set(ref _logHeight, value);
        }

        public ICommand OpenScriptListCommand { get; }
        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }

        public ICommand SaveScriptCommand { get; }
        public ICommand DeleteSelectedScriptsCommand { get; }

        public IInterTabClient InterTabClient { get; }
        public Func<object> NewItemFactory { get; }

        public ScriptsViewManager(
            IUiContext uiContext,
            ScriptEngine engine,
            ISnackbarMessageQueue snackbar)
        {
            InterTabClient = new ScripterInterTabClient(this);

            _uiContext = uiContext;
            _engine = engine;
            _snackbar = snackbar;

            _scripts = new ObservableCollection<ScriptViewModel>();
            Scripts = CollectionViewSource.GetDefaultView(_scripts);
            Scripts.SortDescriptions.Add(new SortDescription(nameof(ScriptViewModel.Group), ListSortDirection.Ascending));
            Scripts.SortDescriptions.Add(new SortDescription(nameof(ScriptViewModel.Name), ListSortDirection.Ascending));
            Scripts.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ScriptViewModel.Group)));

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

            SaveScriptCommand = new RelayCommand(SaveCurrentScript);
            DeleteSelectedScriptsCommand = new RelayCommand(DeleteSelectedScripts);

            NewItemFactory = () => CreateNewScript();

            LoadScripts();

            _fileUpdate
                .AsObservable()
                .Buffer(TimeSpan.FromMilliseconds(200))
                .Subscribe(HandleFileChanges);

            _fsw = new FileSystemWatcher(Engine.ScriptDirectory, "*.csx");
            _fsw.Created += (s, e) => _fileUpdate.OnNext(e);
            _fsw.Changed += (s, e) => _fileUpdate.OnNext(e);
            _fsw.Deleted += (s, e) => _fileUpdate.OnNext(e);
            _fsw.EnableRaisingEvents = true;
        }

        private void HandleFileChanges(IList<FileSystemEventArgs> changes)
        {
            foreach (var g in changes.GroupBy(x => x.Name))
            {
                System.Diagnostics.Debug.WriteLine($"{g.Key}: {string.Join(", ", g.Select(x => x.ChangeType.ToString()))}");
            }
        }

        private void OpenScriptList() => SelectedTabIndex = -1;

        private void LoadScripts()
        {
            DirectoryInfo directory = new(Engine.ScriptDirectory);
            if (!directory.Exists) return;

            foreach (FileInfo file in directory.EnumerateFiles("*.csx", SearchOption.TopDirectoryOnly))
            {
                ScriptModel model = new ScriptModel
                {
                    FileName = file.Name,
                    Name = Path.GetFileNameWithoutExtension(file.Name)
                };

                ScriptGroupViewModel? groupViewModel;

                foreach (string line in File.ReadLines(file.FullName))
                {
                    Match match = ScriptEngine.NameRegex.Match(line);
                    if (match.Success)
                    {
                        model.Name = match.Groups["name"].Value;
                    }

                    match = ScriptEngine.GroupRegex.Match(line);
                    if (match.Success)
                    {
                        string name = match.Groups["group"].Value;
                        if (!_groups.TryGetValue(name, out groupViewModel))
                            _groups[name] = new ScriptGroupViewModel() { Name = name };
                        model.GroupName = name;
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
            OpenTabs.Add(scriptViewModel);

            _uiContext.InvokeAsync(() => SelectedTabItem = scriptViewModel);
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

            SelectedTabItem = script;
        }

        private async void SaveCurrentScript()
        {
            if (SelectedTabItem is not ScriptViewModel script) return;

            if (!script.IsLoaded) return;

            try
            {
                if (script.IsModified)
                {
                    if (script.IsSavedToDisk)
                    {
                        script.Save();
                    }
                    else
                    {
                        string fileName = script.FileName;
                        if (fileName.EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
                            fileName = fileName[..^4];

                        TextInputModalViewModel textInputModal = new TextInputModalViewModel()
                        {
                            Message = "Enter a file name.",
                            InputText = fileName,
                            InputSuffix = ".csx"
                        };

                        textInputModal.ValidateInput = fileName =>
                        {
                            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                                return new ValidationResult("File name contains invalid characters.");
                            if (File.Exists(Path.Combine(Engine.ScriptDirectory, fileName)))
                                return new ValidationResult("A file with that name already exists.");
                            return ValidationResult.Success;
                        };

                        object? result = await DialogHost.Show(textInputModal);
                        if (result is not bool booleanResult || !booleanResult) return;

                        fileName = $"{textInputModal.InputText}.csx";

                        string filePath = Path.Combine(Engine.ScriptDirectory, fileName);

                        if (File.Exists(filePath))
                        {
                            _snackbar.Enqueue("A script with that file name already exists.");
                            return;
                        }

                        script.FileName = Path.GetFileName(filePath);
                        script.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                _snackbar.Enqueue($"Failed to save file to disk: {ex.Message}");
            }
        }

        private async void DeleteSelectedScripts()
        {
            List<ScriptViewModel> selectedScripts = SelectedItems.OfType<ScriptViewModel>().ToList();
            if (selectedScripts.Count == 0) return;

            if (selectedScripts.Any(x => x.IsRunning))
            {
                await DialogHost.Show(new MessageBoxViewModel
                {
                    Buttons = MessageBoxButton.OK,
                    Message = "Cannot delete scripts while they are running."
                });
                return;
            }

            var result = await DialogHost.Show(new MessageBoxViewModel
            {
                Buttons = MessageBoxButton.YesNo,
                Message = $"Delete {"script".ToQuantity(selectedScripts.Count)}?"
            });

            if (result is not MessageBoxResult messageBoxResult ||
                messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                foreach (ScriptViewModel script in selectedScripts)
                {
                    _scripts.Remove(script);
                    OpenTabs.Remove(script);

                    if (script.IsSavedToDisk)
                    {
                        File.Delete(Path.Combine(Engine.ScriptDirectory, script.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                _snackbar.Enqueue($"Failed to remove script(s): {ex.Message}");
            }
        }

        public void DeleteScript(ScriptViewModel script)
        {
            _scripts.Remove(script);

            OpenTabs.Remove(script);
        }

        public void CloseScript(ScriptViewModel script)
        {
            if (SelectedTabIndex != -1)
            {
                if (SelectedTabItem == script)
                {
                    if (SelectedTabIndex == (OpenTabs.Count - 1))
                    {
                        SelectedTabIndex--;
                    }
                    else
                    {
                        SelectedTabIndex++;
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
            if (SelectedTabItem is ScriptViewModel scriptViewModel)
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

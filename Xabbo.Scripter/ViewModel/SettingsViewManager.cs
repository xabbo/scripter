using System.IO;
using System.Text.Json;

using GalaSoft.MvvmLight;

using Wpf.Ui.Appearance;

using Xabbo.Scripter.Configuration;

namespace Xabbo.Scripter.ViewModel;

public class SettingsViewManager : ObservableObject
{
    const string FilePath = "settings.json";

    // private Settings _previousSettings;
    private Settings _settings;

    public bool DarkMode
    {
        get => _settings.DarkMode;
        set
        {
            if (_settings.DarkMode == value) return;

            _settings.DarkMode = value;
            Theme.Apply(value ? ThemeType.Dark : ThemeType.Light, updateAccent: false);
            Save();

            RaisePropertyChanged();
        }
    }

    public bool ShowUserName
    {
        get => _settings.ShowUserName;
        set
        {
            if (_settings.ShowUserName == value) return;

            _settings.ShowUserName = value;
            Save();

            RaisePropertyChanged();
        }
    }

    public SettingsViewManager()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            _settings = JsonSerializer.Deserialize<Settings>(json)
                ?? throw new IOException("Failed to load settings.");
        }
        else
        {
            _settings = new Settings();
        }
    }

    private void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(_settings)); } catch { }
    }
}

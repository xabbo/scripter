using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

using Xabbo.Scripter.ViewModel;

namespace Xabbo.Scripter.Services;

public class ScripterUiManager : IUiManager
{
    private readonly Application _application;
    private readonly IUiContext _uiContext;
    private readonly SettingsViewManager _settings;

    public ScripterUiManager(
        Application application,
        IUiContext uiContext,
        SettingsViewManager settings)
    {
        _application = application;
        _uiContext = uiContext;
        _settings = settings;
    }

    public void SetDarkTheme(bool isDarkTheme)
    {
        _uiContext.Invoke(() => _settings.DarkMode = isDarkTheme);
    }

    public void SetBackgroundColor(string hexColor)
    {
        if (hexColor.StartsWith("#"))
            hexColor = hexColor[1..];

        if (!Regex.IsMatch(hexColor, @"(?i)^[0-9a-f]{6}$"))
            throw new FormatException($"Hex color is of an invalid format: \"{hexColor}\".");

        int color = int.Parse(hexColor, NumberStyles.HexNumber);
        SetBackgroundColor(
            (byte)((color >> 16) & 0xFF),
            (byte)((color >> 8) & 0xFF),
            (byte)(color & 0xFF)
        );
    }

    public void SetBackgroundColor(byte red, byte green, byte blue)
    {
        _uiContext.Invoke(() =>
        {
            Color color = new() { R = red, G = green, B = blue, A = 255 };
            SolidColorBrush brush = new(color);

            _application.Resources["ApplicationBackgroundColor"] = color;
            _application.Resources["ApplicationBackgroundBrush"] = brush;
        });
    }
}

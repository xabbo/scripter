using System;

namespace Xabbo.Scripter.Services;

public interface IUiManager
{
    void SetDarkTheme(bool isDarkTheme);
    void SetBackgroundColor(string hexColor);
    void SetBackgroundColor(byte red, byte green, byte blue);
}

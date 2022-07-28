using System;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Sets the theme of the scripter UI.
    /// </summary>
    /// <param name="isDarkTheme">Whether to use dark theme or not.</param>
    public void SetDarkTheme(bool isDarkTheme) => _scriptHost.UiManager.SetDarkTheme(isDarkTheme);

    /// <summary>
    /// Sets the background color of the scripter UI.
    /// </summary>
    public void SetBackgroundColor(byte red, byte green, byte blue) => _scriptHost.UiManager.SetBackgroundColor(red, green, blue);

}

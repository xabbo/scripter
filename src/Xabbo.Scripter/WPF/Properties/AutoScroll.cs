using System;
using System.Windows;
using System.Windows.Controls;

namespace Xabbo.Scripter.WPF.Properties;

public static class AutoScroll
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(AutoScroll),
        new PropertyMetadata(false, IsEnabledChanged)
    );

    public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject d, bool value) => d.SetValue(IsEnabledProperty, value);

    private static void IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue == e.NewValue) return;

        if (d is TextBox textBox && e.NewValue is bool isEnabled)
        {
            if (isEnabled)
            {
                textBox.TextChanged += ScrollToEnd;
            }
            else
            {
                textBox.TextChanged -= ScrollToEnd;
            }
        }
    }

    private static void ScrollToEnd(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.ScrollToEnd();
        }
    }
}

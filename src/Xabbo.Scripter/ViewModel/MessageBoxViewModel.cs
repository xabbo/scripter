using System;
using System.Windows;

using GalaSoft.MvvmLight;

namespace Xabbo.Scripter.ViewModel;

public class MessageBoxViewModel : ObservableObject
{
    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    private MessageBoxButton _buttons = MessageBoxButton.OK;
    public MessageBoxButton Buttons
    {
        get => _buttons;
        set
        {
            if (Set(ref _buttons, value))
            {
                RaisePropertyChanged(nameof(ShowCancelButton));
                RaisePropertyChanged(nameof(CancelButtonText));
                RaisePropertyChanged(nameof(CancelResult));

                RaisePropertyChanged(nameof(ShowDeclineButton));
                RaisePropertyChanged(nameof(DeclineButtonText));
                RaisePropertyChanged(nameof(DeclineResult));

                RaisePropertyChanged(nameof(ConfirmButtonText));
                RaisePropertyChanged(nameof(ConfirmResult));
            }
        }
    }

    public bool ShowCancelButton => Buttons switch
    {
        MessageBoxButton.OKCancel or
        MessageBoxButton.YesNoCancel => true,
        _ => false
    };

    public bool ShowDeclineButton => Buttons switch
    {
        MessageBoxButton.YesNo or
        MessageBoxButton.YesNoCancel => true,
        _ => false
    };

    public string CancelButtonText => "Cancel";
    public string DeclineButtonText => "No";
    public string ConfirmButtonText => Buttons switch
    {
        MessageBoxButton.OK or
        MessageBoxButton.OKCancel => "OK",
        MessageBoxButton.YesNo or
        MessageBoxButton.YesNoCancel => "Yes",
        _ => string.Empty
    };

    public MessageBoxResult CancelResult => MessageBoxResult.Cancel;
    public MessageBoxResult DeclineResult => MessageBoxResult.No;
    public MessageBoxResult ConfirmResult => Buttons switch
    {
        MessageBoxButton.OK or
        MessageBoxButton.OKCancel => MessageBoxResult.OK,
        MessageBoxButton.YesNo or
        MessageBoxButton.YesNoCancel => MessageBoxResult.Yes,
        _ => MessageBoxResult.None
    };

    public MessageBoxViewModel() { }
}

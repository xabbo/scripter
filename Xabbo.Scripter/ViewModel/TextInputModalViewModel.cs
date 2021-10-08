using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using MaterialDesignThemes.Wpf;

namespace Xabbo.Scripter.ViewModel
{
    public class TextInputModalViewModel : ObservableObject
    {
        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set
            {
                if (ValidateInput is not null)
                {
                    ValidationResult? validationResult = ValidateInput(value);
                    if (validationResult != ValidationResult.Success)
                    {
                        IsInputValid = false;
                        throw new Exception(validationResult?.ErrorMessage);
                    }
                }

                Set(ref _inputText, value);
                IsInputValid = true;
            }
        }

        private string _inputSuffix = string.Empty;
        public string InputSuffix
        {
            get => _inputSuffix;
            set => Set(ref _inputSuffix, value);
        }

        private bool _isInputValid = true;
        public bool IsInputValid
        {
            get => _isInputValid;
            set => Set(ref _isInputValid, value);
        }

        public Func<string, ValidationResult?> ValidateInput { get; set; } = x => ValidationResult.Success;

        public ICommand SubmitCommand { get; }

        public TextInputModalViewModel()
        {
            SubmitCommand = new RelayCommand(OnSubmit, CanSubmit);
        }

        private void OnSubmit()
        {
            if (IsInputValid)
            {
                DialogHost.Close("Root", true);
            }
        }

        public bool CanSubmit() => IsInputValid;
    }
}

using System;

using GalaSoft.MvvmLight;

namespace Xabbo.Scripter.ViewModel
{
    public class ScriptGroupViewModel : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }
    }
}

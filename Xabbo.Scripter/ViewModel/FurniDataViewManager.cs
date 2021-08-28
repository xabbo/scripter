using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;

using GalaSoft.MvvmLight;

using Xabbo.Core.GameData;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.ViewModel
{
    public class FurniDataViewManager : ObservableObject
    {
        private readonly IUIContext _uiContext;
        private readonly IGameDataManager _gameDataManager;

        private readonly ObservableCollection<FurniInfoViewModel> _furni = new();
        public ICollectionView Furni { get; }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                    RefreshList();
            }
        }

        public FurniDataViewManager(IUIContext uiContext, IGameDataManager gameDataManager)
        {
            _uiContext = uiContext;
            _gameDataManager = gameDataManager;

            Furni = CollectionViewSource.GetDefaultView(_furni);
            Furni.Filter = Filter;

            Task initializeTask = Task.Run(InitializeAsync);
        }

        private void RefreshList()
        {
            if (!_uiContext.IsSynchronized)
            {
                _uiContext.InvokeAsync(() => RefreshList());
                return;
            }

            Furni.Refresh();
        }

        private bool Filter(object o)
        {
            if (o is not FurniInfoViewModel furniInfo) return false;

            if (string.IsNullOrWhiteSpace(_filterText))
            {
                return true;
            }
            else
            {
                return
                    furniInfo.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                    furniInfo.Identifier.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
            }
        }

        private async Task InitializeAsync()
        {
            FurniData furniData = await _gameDataManager.GetFurniDataAsync();

            await _uiContext.InvokeAsync(() =>
            {
                foreach (var info in furniData)
                {
                    _furni.Add(new FurniInfoViewModel(info));
                }
            });
        }
    }
}

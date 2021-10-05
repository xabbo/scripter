using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

using GalaSoft.MvvmLight;

using Xabbo.Core.GameData;

using Xabbo.Scripter.Services;

namespace Xabbo.Scripter.ViewModel
{
    public class FurniDataViewManager : ObservableObject
    {
        private readonly IUiContext _uiContext;
        private readonly IGameDataManager _gameDataManager;

        private ObservableCollection<FurniInfoViewModel> _furni = null!;
        public ICollectionView Furni { get; private set; } = null!;

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

        public FurniDataViewManager(IUiContext uiContext, IGameDataManager gameDataManager)
        {
            _uiContext = uiContext;
            _gameDataManager = gameDataManager;

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
                _furni = new ObservableCollection<FurniInfoViewModel>(
                    furniData.Select(x => new FurniInfoViewModel(x))
                );
                Furni = CollectionViewSource.GetDefaultView(_furni);
                Furni.Filter = Filter;
                RaisePropertyChanged(nameof(Furni));
            });
        }
    }
}

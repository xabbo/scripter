using System;

using GalaSoft.MvvmLight;

namespace Xabbo.Scripter.ViewModel;

public class ToolsViewManager : ObservableObject
{
    public FurniDataViewManager FurniData { get; }

    public ToolsViewManager(FurniDataViewManager furniData)
    {
        FurniData = furniData;
    }
}

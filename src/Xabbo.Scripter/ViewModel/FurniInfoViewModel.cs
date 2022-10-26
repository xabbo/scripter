using System;

using GalaSoft.MvvmLight;

using Xabbo.Core;
using Xabbo.Core.GameData;

namespace Xabbo.Scripter.ViewModel;

public class FurniInfoViewModel : ObservableObject
{
    public FurniInfo Info { get; }

    public string Name => Info.Name;
    public string Identifier => Info.Identifier;
    public ItemType Type => Info.Type;
    public int Kind => Info.Kind;
    public string Line => Info.Line;
    public FurniCategory Category => Info.Category;
    public string CategoryName => Info.CategoryName;

    public FurniInfoViewModel(FurniInfo info)
    {
        Info = info;
    }
}

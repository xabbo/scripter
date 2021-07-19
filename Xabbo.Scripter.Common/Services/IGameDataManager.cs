using System;
using System.Threading.Tasks;

using Xabbo.Core.GameData;

namespace Xabbo.Scripter.Services
{
    public interface IGameDataManager
    {
        FurniData? FurniData { get; }
        FigureData? FigureData { get; }
        ProductData? ProductData { get; }
        ExternalTexts? ExternalTexts { get; }

        Task<FigureData> GetFigureDataAsync();
        Task<FurniData> GetFurniDataAsync();
        Task<ProductData> GetProductDataAsync();
        Task<ExternalTexts> GetExternalTextsAsync();

        Task UpdateAsync();
    }
}

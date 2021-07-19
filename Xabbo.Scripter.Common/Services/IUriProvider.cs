using System;

namespace Xabbo.Scripter.Services
{
    public interface IUriProvider<TEndpoints>
        where TEndpoints : Enum
    {
        string Domain { get; }
        Uri this[TEndpoints endpoint] { get; }
        Uri GetUri(TEndpoints endpoint);
    }
}

using System;

namespace Xabbo.Scripter.Services;

public interface IJsonSerializer
{
    string Serialize<T>(T value, bool indented = true);
    T? Deserialize<T>(string json);
}

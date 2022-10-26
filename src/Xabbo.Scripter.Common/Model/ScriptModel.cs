using System;

namespace Xabbo.Scripter.Model;

public record ScriptModel
{
    public int DocumentId { get; set; }
    public string FileName { get; set; }
    public string Name { get; set; }
    public string GroupName { get; set; }

    public ScriptModel()
    {
        FileName =
        Name =
        GroupName = string.Empty;
    }
}

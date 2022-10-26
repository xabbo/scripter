using System;
using System.Collections.Generic;

namespace Xabbo.Scripter.Engine;

#nullable disable

public class ScriptEngineOptions
{
    public List<string> References { get; set; }
    public List<string> Imports { get; set; }
}

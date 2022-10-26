using System;
using Xabbo.Extension;
using Xabbo.GEarth;
using Xabbo.Messages;

namespace Xabbo.Scripter;

[Title("xabbo scripter")]
[Description("C# scripting interface")]
[Author("b7")]
public class ScripterGEarthExtension : GEarthExtension
{
    public ScripterGEarthExtension(IMessageManager messages, GEarthOptions options)
        : base(messages, options)
    { }
}

using System;

using Xabbo.GEarth;
using Xabbo.Messages;

namespace Xabbo.Scripter;

public class ScripterExtension : GEarthExtension
{
    public ScripterExtension(IMessageManager messages, GEarthOptions options)
        : base(messages, options)
    { }
}

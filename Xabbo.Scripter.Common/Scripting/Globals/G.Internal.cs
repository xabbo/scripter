using System;
using System.Diagnostics;

using Xabbo.Core.Game;

namespace Xabbo.Scripter.Scripting;

/*
 * Internal helper methods used within the globals.
 */ 
public partial class G
{
    private IRoom RequireRoom() => Room ?? throw new InvalidOperationException("The user is not in a room.");
}

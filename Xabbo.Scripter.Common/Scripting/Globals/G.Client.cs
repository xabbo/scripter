using System;

using Xabbo.Interceptor;
using Xabbo.Core;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Shows a client-side chat bubble.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="index">
    /// The index of the entity to display the chat bubble from.
    /// If set to <c>-1</c>, this will attempt to use the user's own index.
    /// </param>
    /// <param name="bubble">The bubble style.</param>
    /// <param name="type">The type of chat bubble to display.</param>
    public void ShowBubble(string message, int index = -1, int bubble = 30, ChatType type = ChatType.Whisper)
    {
        if (index == -1) index = Self?.Index ?? -1;

        Interceptor.Send(In.Whisper, index, message, 0, bubble, 0, 0);
    }
}

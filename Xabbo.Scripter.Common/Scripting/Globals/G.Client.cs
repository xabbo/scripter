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
    /// If the argument is <see langword="null" />, the user's own index will be used, if available.
    /// </param>
    /// <param name="bubble">The bubble style.</param>
    /// <param name="type">The type of chat bubble to display.</param>
    public void ShowBubble(string message, int? index = null, int bubble = 30, ChatType type = ChatType.Whisper)
    {
        Interceptor.Send(
            type switch
            {
                ChatType.Whisper => In.Whisper,
                ChatType.Talk => In.Chat,
                ChatType.Shout => In.Shout,
                _ => throw new ArgumentException("Invalid chat type specified.", nameof(type))
            },
            index ?? Self?.Index ?? -1,
            message, 0, bubble, 0, 0
        );
    }
}

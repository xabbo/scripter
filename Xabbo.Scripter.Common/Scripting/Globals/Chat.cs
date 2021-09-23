using System;

using Xabbo.Core;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Sends a chat message with the specified message and chat bubble style.
        /// </summary>
        public void Chat(ChatType chatType, string message, int bubble = 0)
        {
            switch (chatType)
            {
                case ChatType.Talk:
                    Send(Out.Chat, message, bubble, -1);
                    break;
                case ChatType.Shout:
                    Send(Out.Shout, message, bubble);
                    break;
                case ChatType.Whisper:
                    Send(Out.Whisper, message, bubble, -1);
                    break;
                default:
                    throw new Exception($"Unknown chat type: {chatType}.");
            }
        }

        /// <summary>
        /// Whispers a user with the specified message and chat bubble style.
        /// </summary>
        public void Whisper(IRoomUser recipient, string message, int bubble = 0)
            => Chat(ChatType.Whisper, $"{recipient.Name} {message}", bubble);

        /// <summary>
        /// Whispers a user with the specified message and chat bubble style.
        /// </summary>
        public void Whisper(string recipient, string message, int bubble = 0)
            => Chat(ChatType.Whisper, $"{recipient} {message}", bubble);

        /// <summary>
        /// Talks with the specified message and chat bubble style.
        /// </summary>
        public void Talk(string message, int bubble = 0)
            => Chat(ChatType.Talk, message, bubble);

        /// <summary>
        /// Shouts with the specified message and chat bubble style.
        /// </summary>
        public void Shout(string message, int bubble = 0)
            => Chat(ChatType.Shout, message, bubble);
    }
}

using System;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Activates the specified effect. (Warning: this will consume the effect if it is not permanent)
        /// </summary>
        public void ActivateEffect(int effectId) => Send(Out.ActivateAvatarEffect, effectId);

        /// <summary>
        /// Enables the specified effect.
        /// </summary>
        public void EnableEffect(int effectId) => Send(Out.UseAvatarEffect, effectId);

        /// <summary>
        /// Disables the current effect.
        /// </summary>
        public void DisableEffect() => EnableEffect(-1);
    }
}

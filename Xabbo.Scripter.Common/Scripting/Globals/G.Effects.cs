using System;

using Xabbo.Interceptor;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Activates the specified effect. (Warning: this will consume the effect if it is not permanent)
        /// </summary>
        public void ActivateEffect(int effectId) => Interceptor.Send(Out.ActivateAvatarEffect, effectId);

        /// <summary>
        /// Enables the specified effect.
        /// </summary>
        public void EnableEffect(int effectId) => Interceptor.Send(Out.UseAvatarEffect, effectId);

        /// <summary>
        /// Disables the current effect.
        /// </summary>
        public void DisableEffect() => EnableEffect(-1);
    }
}

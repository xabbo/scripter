using System;

using Xabbo.Core;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Makes the user perform the specified action.
        /// </summary>
        public void Action(int action) => Send(Out.Expression, action);

        /// <summary>
        /// Makes the user perform the specified action.
        /// </summary>
        public void Action(Actions action) => Action((int)action);

        /// <summary>
        /// Makes the user unidle.
        /// </summary>
        public void Unidle() => Action(Actions.None);

        /// <summary>
        /// Makes the user wave.
        /// </summary>
        public void Wave() => Action(Actions.Wave);

        /// <summary>
        /// Makes the user idle.
        /// </summary>
        public void Idle() => Action(Actions.Idle);

        /// <summary>
        /// Makes the user thumbs up.
        /// </summary>
        public void ThumbsUp() => Action(Actions.ThumbsUp);

        /// <summary>
        /// Makes the user sit if <c>true</c>, or stand if <c>false</c> is passed in.
        /// </summary>
        /// <param name="sit"><c>true</c> to sit, or <c>false</c> to stand.</param>
        public void Sit(bool sit) => Send(Out.Posture, sit ? 1 : 0);

        /// <summary>
        /// Makes the user sit.
        /// </summary>
        public void Sit() => Send(Out.Posture, 1);

        /// <summary>
        /// Makes the user stand.
        /// </summary>
        public void Stand() => Send(Out.Posture, 0);

        /// <summary>
        /// Makes the user show the specified sign.
        /// </summary>
        public void Sign(int sign) => Send(Out.ShowSign, sign);

        /// <summary>
        /// Makes the user show the specified sign.
        /// </summary>
        public void Sign(Signs sign) => Sign((int)sign);

        /// <summary>
        /// Makes the user perform the specfied dance.
        /// </summary>
        public void Dance(int dance) => Send(Out.Dance, dance);

        /// <summary>
        /// Makes the user perform the specfied dance.
        /// </summary>
        public void Dance(Dances dance) => Dance((int)dance);

        /// <summary>
        /// Makes the user dance.
        /// </summary>
        public void Dance() => Dance(1);

        /// <summary>
        /// Makes the user stop dancing.
        /// </summary>
        public void StopDancing() => Dance(0);
    }
}

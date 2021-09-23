using System;

using Xabbo.Core;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(int x, int y) => Send(Out.Move, x, y);

        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move((int X, int Y) location) => Move(location.X, location.Y);

        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(Tile location) => Move(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(int x, int y) => Send(Out.LookTo, x, y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo((int X, int Y) location) => LookTo(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(Tile location) => LookTo(location.X, location.Y);

        /// <summary>
        /// Makes the user look to the specified direction.
        /// </summary>
        public void Turn(int dir) => LookTo(H.GetMagicVector(dir));

        /// <summary>
        /// Makes the user look to the specified direction.
        /// </summary>
        public void Turn(Directions dir) => Turn((int)dir);
    }
}

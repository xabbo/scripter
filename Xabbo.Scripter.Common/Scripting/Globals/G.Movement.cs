using System;

using Xabbo.Interceptor;
using Xabbo.Core;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(int x, int y) => Interceptor.Send(Out.Move, x, y);

        /// <summary>
        /// Moves to the specified location.
        /// </summary>
        public void Move(Point location) => Move(location.X, location.Y);

        /// <summary>
        /// Moves to the location of the specified floor entity.
        /// If the floor entity occupies more than one tile, a random tile will be chosen.
        /// </summary>
        /// <param name="floorEntity"></param>
        public void Move(IFloorEntity floorEntity) => Move(Rand(floorEntity.Area));

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(int x, int y) => Interceptor.Send(Out.LookTo, x, y);

        /// <summary>
        /// Makes the user look to the specified location.
        /// </summary>
        public void LookTo(Point location) => LookTo(location.X, location.Y);

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

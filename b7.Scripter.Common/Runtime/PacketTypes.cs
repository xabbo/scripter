using System;
using System.ComponentModel;

namespace b7.Scripter.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PacketTypes
    {
        public static readonly Type
            Byte = typeof(byte),
            Bool = typeof(bool),
            Short = typeof(short),
            Int = typeof(int),
            Float = typeof(float),
            Long = typeof(long),
            Str = typeof(string),
            ByteArray = typeof(byte[]);
    }
}

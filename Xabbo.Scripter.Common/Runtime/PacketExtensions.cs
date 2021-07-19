using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xabbo.Messages;

namespace Xabbo.Scripter.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PacketExtensions
    {
        public static T Read<T>(this IReadOnlyPacket packet)
        {
            Type t = typeof(T);
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Boolean: return (T)Convert.ChangeType(packet.ReadBool(), t);
                case TypeCode.Byte: return (T)Convert.ChangeType(packet.ReadByte(), t);
                case TypeCode.Int16: return (T)Convert.ChangeType(packet.ReadShort(), t);
                case TypeCode.Int32: return (T)Convert.ChangeType(packet.ReadInt(), t);
                case TypeCode.Int64: return (T)Convert.ChangeType(packet.ReadLong(), t);
                case TypeCode.String: return (T)Convert.ChangeType(packet.ReadString(), t);
                case TypeCode.Single: return (T)Convert.ChangeType(packet.ReadFloat(), t);
                default:
                {
                    if (t == typeof(LegacyLong)) return (T)Convert.ChangeType(packet.ReadLegacyLong(), t);
                    else if (t == typeof(LegacyShort)) return (T)Convert.ChangeType(packet.ReadLegacyShort(), t);
                    else if (t == typeof(LegacyFloat)) return (T)Convert.ChangeType(packet.ReadLegacyFloat(), t);
                    throw new Exception($"Invalid type specified: {typeof(T)}.");
                }
            };
        }

        public static (T1, T2)
        Read<T1, T2>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p)
        );

        public static (T1, T2, T3)
        Read<T1, T2, T3>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p)
        );

        public static (T1, T2, T3, T4)
        Read<T1, T2, T3, T4>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p)
        );

        public static (T1, T2, T3, T4, T5)
        Read<T1, T2, T3, T4, T5>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p)
        );

        public static (T1, T2, T3, T4, T5, T6)
        Read<T1, T2, T3, T4, T5, T6>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7)
        Read<T1, T2, T3, T4, T5, T6, T7>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8)
        Read<T1, T2, T3, T4, T5, T6, T7, T8>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB, TC)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB, TC>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p), Read<TC>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB, TC, TD)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB, TC, TD>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p), Read<TC>(p),
            Read<TD>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB, TC, TD, TE)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB, TC, TD, TE>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p), Read<TC>(p),
            Read<TD>(p), Read<TE>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB, TC, TD, TE, TF)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB, TC, TD, TE, TF>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p), Read<TC>(p),
            Read<TD>(p), Read<TE>(p), Read<TF>(p)
        );

        public static (T1, T2, T3, T4, T5, T6, T7, T8,
                       T9, TA, TB, TC, TD, TE, TF, T10)
        Read<T1, T2, T3, T4, T5, T6, T7, T8, T9, TA, TB, TC, TD, TE, TF, T10>(this IReadOnlyPacket p) => (
            Read<T1>(p), Read<T2>(p), Read<T3>(p), Read<T4>(p),
            Read<T5>(p), Read<T6>(p), Read<T7>(p), Read<T8>(p),
            Read<T9>(p), Read<TA>(p), Read<TB>(p), Read<TC>(p),
            Read<TD>(p), Read<TE>(p), Read<TF>(p), Read<T10>(p)
        );

    }
}

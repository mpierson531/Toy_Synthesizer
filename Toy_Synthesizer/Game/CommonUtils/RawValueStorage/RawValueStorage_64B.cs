using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Toy_Synthesizer.Game.CommonUtils.RawValueStorage
{
    [StructLayout(LayoutKind.Sequential, Size = STORAGE_SIZE)]
    // Provides 64 bytes of raw value storage for any unmanaged type.
    // Will not work for reading/writing different primitive types to the same instance.
    // For example, int -> float or float -> int will not work correctly.
    public unsafe struct RawValueStorage_64B : IRawValueStorage
    {
        public const int STORAGE_SIZE = 64;

        static int IRawValueStorage.StorageSize => STORAGE_SIZE;

        public static RawValueStorage_64B From<T>(T value) where T : unmanaged
        {
            return ValueStorageUtils.From<T, RawValueStorage_64B>(value);
        }

        public ref ulong StorageAddress => ref segment0;

        private ulong segment0;
        private ulong segment1;
        private ulong segment2;
        private ulong segment3;
        private ulong segment4;
        private ulong segment5;
        private ulong segment6;
        private ulong segment7;

        public T Read<T>() where T : unmanaged
        {
            return ValueStorageUtils.Read<T, RawValueStorage_64B>(ref this);
        }

        // Will attempt to fill the buffer.
        public void ReadBuffer<T>(Span<T> buffer, out int realCount) where T : unmanaged
        {
            ValueStorageUtils.ReadBuffer(ref this, buffer, out realCount);
        }

        public void ReadBuffer<T>(Span<T> buffer, int sourceOffset, int destinationOffset, int requestedCount, out int realCount) where T : unmanaged
        {
            ValueStorageUtils.ReadBuffer(ref this, buffer, sourceOffset, destinationOffset, requestedCount, out realCount);
        }

        public void Write<T>(T value) where T : unmanaged
        {
            ValueStorageUtils.Write(ref this, value);
        }

        public void WriteBuffer<T>(Span<T> buffer, int offset, int count) where T : unmanaged
        {
            ValueStorageUtils.WriteBuffer(ref this, buffer, offset, count);
        }

        public void WriteBuffer<T>(Span<T> buffer) where T : unmanaged
        {
            ValueStorageUtils.WriteBuffer(ref this, buffer);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.CommonUtils.RawValueStorage
{
    public unsafe static class ValueStorageUtils
    {
        public static E FromBuffer<T, E>(Span<T> buffer) where T : unmanaged where E : struct, IRawValueStorage
        {
            E result = default;

            result.WriteBuffer(buffer);

            return result;
        }

        public static E FromBuffer<T, E>(Span<T> buffer, int offset, int count) where T : unmanaged where E : struct, IRawValueStorage
        {
            E result = default;

            result.WriteBuffer(buffer, offset, count);

            return result;
        }

        public static E From<T, E>(T value) where T : unmanaged where E : struct, IRawValueStorage
        {
            E result = default;

            result.Write(value);

            return result;
        }

        public static T Read<T, E>(ref E storage) where T : unmanaged where E : struct, IRawValueStorage
        {
            return Read<T>(ref storage.StorageAddress, E.StorageSize);
        }

        public static void Write<T, E>(ref E storage, T value) where T : unmanaged where E : struct, IRawValueStorage
        {
            Write<T>(ref storage.StorageAddress, value, E.StorageSize);
        }

        // Will attempt to fill the buffer.
        public static void ReadBuffer<T, E>(ref E storage, Span<T> buffer, out int realCount) where T : unmanaged where E : struct, IRawValueStorage
        {
            ReadBuffer<T>(buffer, ref storage.StorageAddress, E.StorageSize, out realCount);
        }

        public static void ReadBuffer<T, E>(ref E storage, Span<T> buffer, int sourceOffset, int destinationOffset, int requestedCount, 
                                            out int realCount)
            where T : unmanaged where E : struct, IRawValueStorage
        {
            ReadBuffer<T>(buffer, sourceOffset, destinationOffset, requestedCount, ref storage.StorageAddress, E.StorageSize, out realCount);
        }

        public static void WriteBuffer<T, E>(ref E storage, Span<T> buffer, int offset, int count) where T : unmanaged where E : struct, IRawValueStorage
        {
            WriteBuffer(buffer, sourceOffset: offset, count: count, storage: ref storage.StorageAddress, storageSize: E.StorageSize);
        }

        public static void WriteBuffer<T, E>(ref E storage, Span<T> buffer) where T : unmanaged where E : struct, IRawValueStorage
        {
            ValueStorageUtils.WriteBuffer(buffer, sourceOffset: 0, count: buffer.Length, storage: ref storage.StorageAddress, storageSize: E.StorageSize);
        }

        public static T Read<T>(ref ulong storage, int storageSize) where T : unmanaged
        {
            ValueStorageUtils.ValidateSizeOfType<T>(storageSize);

            return Unsafe.As<ulong, T>(ref storage);
        }

        public static void Write<T>(ref ulong storage, T value, int storageSize) where T : unmanaged
        {
            ValidateSizeOfType<T>(storageSize);

            Unsafe.As<ulong, T>(ref storage) = value;
        }

        public static void WriteBuffer<T>(Span<T> buffer, int sourceOffset, int count, ref ulong storage, int storageSize) where T : unmanaged
        {
            ValueStorageUtils.ValidateSizeOfType<T>(storageSize);

            if (count < 0)
            {
                throw new InvalidOperationException("count was less than zero.");
            }

            if (sourceOffset + count > buffer.Length)
            {
                throw new InvalidOperationException("offset plus count was greater than buffer.Length.");
            }

            if (count == 0)
            {
                return;
            }

            WriteBufferRaw(buffer, sourceOffset, count, ref storage, storageSize);
        }

        public static void WriteBuffer<T>(Span<T> buffer, ref ulong storage, int storageSize) where T : unmanaged
        {
            WriteBufferRaw(buffer, offset: 0, count: buffer.Length, storage: ref storage, storageSize: storageSize);
        }

        private static void WriteBufferRaw<T>(Span<T> buffer, int offset, int count, ref ulong storage, int storageSize) where T : unmanaged
        {
            int maxItems = storageSize / SizeOf_unmanaged<T>();

            int actualCount = Math.Min(maxItems, count);

            ref T start = ref Unsafe.As<ulong, T>(ref storage);

            for (int index = 0; index < actualCount; index++)
            {
                Unsafe.Add(ref start, index) = buffer[offset + index];
            }
        }

        // Will attempt to fill the buffer.
        public static void ReadBuffer<T>(Span<T> buffer, ref ulong storage, int storageSize,
                                             out int realCount) where T : unmanaged
        {
            ReadBuffer(buffer,
                       sourceOffset: 0,
                       destinationOffset: 0,
                       requestedCount: buffer.Length,
                       storage: ref storage,
                       storageSize: storageSize,
                       realCount: out realCount);
        }

        public static void ReadBuffer<T>(Span<T> buffer, int sourceOffset, int destinationOffset, int requestedCount, ref ulong storage, int storageSize, 
                                             out int realCount) where T : unmanaged
        {
            if (sourceOffset < 0)
            {
                throw new InvalidOperationException("sourceOffset was less than zero.");
            }

            if (destinationOffset < 0)
            {
                throw new InvalidOperationException("destinationOffset was less than zero.");
            }

            int maxItems = (storageSize / SizeOf_unmanaged<T>()) - sourceOffset;

            if (maxItems < 0)
            {
                realCount = 0;

                return;
            }

            realCount = Math.Min(maxItems, requestedCount);

            if (destinationOffset + realCount > buffer.Length)
            {
                realCount = Math.Max(0, buffer.Length - destinationOffset);
            }

            ref T start = ref Unsafe.Add(ref Unsafe.As<ulong, T>(ref storage), sourceOffset);

            ReadOnlySpan<T> sourceBuffer = MemoryMarshal.CreateReadOnlySpan(ref start, realCount);

            sourceBuffer.CopyTo(buffer.Slice(destinationOffset, realCount));
        }

        public static void ValidateSizeOfType<T>(int size, string message = null) where T : unmanaged
        {
            if (size < 0)
            {
                throw new InvalidOperationException("size cannot be less than zero.");
            }

            if (SizeOf_unmanaged<T>() > size)
            {
                if (message is null)
                {
                    message = "Invalid size.";
                }

                throw new InvalidOperationException(message);
            }
        }

        public static int SizeOf_unmanaged<T>() where T : unmanaged
        {
            return sizeof(T);
        }
    }
}

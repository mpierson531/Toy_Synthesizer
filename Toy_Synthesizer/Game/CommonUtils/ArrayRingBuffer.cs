using System;

namespace Toy_Synthesizer.Game.CommonUtils
{
    public class ArrayRingBuffer<T>
    {
        private readonly T[] array;
        private int readIndex;
        private int writeIndex;
        private int count;

        public bool AllowReadLooping { get; set; } = true;

        public int Capacity
        {
            get => array.Length;
        }

        public int CurrentCount
        {
            get => count;
        }

        public bool IsEmpty
        {
            get => CurrentCount == 0;
        }

        public ArrayRingBuffer(int capacity)
        {
            array = new T[capacity];
        }

        public void Write(ReadOnlySpan<T> values)
        {
            ReadOnlySpan<T> source = values.Length > Capacity ? values.Slice(values.Length - Capacity) : values;

            int totalToWrite = source.Length;
            int spaceToEnd = Capacity - writeIndex;

            // TODO: This probably needs improved.
            if (totalToWrite <= spaceToEnd)
            {
                // Fits in one chunk.

                source.CopyTo(array.AsSpan(writeIndex));
            }
            else
            {
                // Split into two chunks; end of array, then wrap to start.

                source.Slice(0, spaceToEnd).CopyTo(array.AsSpan(writeIndex));

                source.Slice(spaceToEnd).CopyTo(array.AsSpan(0));
            }

            writeIndex = (writeIndex + totalToWrite) % Capacity;

            count = Math.Min(Capacity, count + totalToWrite);
        }

        /*public int Read(Span<T> destination)
        {
            if (count == 0)
            {
                return 0;
            }

            int totalRead = 0;
            int remainingToFill = destination.Length;

            *//*while (remainingToFill > 0)
            {
                int toRead = AllowReadLooping ? remainingToFill : count;

                if (toRead <= 0)
                {
                    break;
                }

                int spaceToEnd = Capacity - readIndex;

                int read = Math.Min(toRead, spaceToEnd);

                array.AsSpan(readIndex, read).CopyTo(destination.Slice(totalRead));

                totalRead += read;

                readIndex = (readIndex + read) % Capacity;

                if (!AllowReadLooping)
                {
                    count -= read;
                }

                remainingToFill -= read;
            }

            return totalRead;*/

        /*while (remainingToFill > 0)
        {
            int available = count;

            int toRead = Math.Min(remainingToFill, available);

            if (toRead <= 0)
            {
                break;
            }

            int spaceToEnd = Capacity - readIndex;

            int firstPart = Math.Min(toRead, spaceToEnd);

            array.AsSpan(readIndex, firstPart).CopyTo(destination.Slice(totalRead));

            if (toRead > firstPart)
            {
                int secondPart = toRead - firstPart;

                array.AsSpan(0, secondPart).CopyTo(destination.Slice(totalRead + firstPart));
            }

            readIndex = (readIndex + toRead) % available;

            if (!AllowReadLooping)
            {
                count -= toRead;
            }

            totalRead += toRead;

            remainingToFill -= toRead;

            if (!AllowReadLooping && count <= 0)
            {
                break;
            }
        }

        return totalRead;*//*
    }*/

        public ReadOnlySpan<T> GetReadonlySpan()
        {
            return new ReadOnlySpan<T>(array, readIndex, count);
        }

        public int Read(Span<T> destination)
        {
            if (count == 0)
            {
                return 0;
            }

            int totalRead = 0;
            int remainingToFill = destination.Length;

            int wrapBoundary = AllowReadLooping ? count : Capacity;

            while (remainingToFill > 0)
            {
                if (!AllowReadLooping && count <= 0)
                {
                    break;
                }

                int spaceToEnd = wrapBoundary - readIndex;

                int toRead = Math.Min(remainingToFill, spaceToEnd);

                if (!AllowReadLooping)
                {
                    toRead = Math.Min(toRead, count);
                }

                if (toRead <= 0)
                {
                    break;
                }

                array.AsSpan(readIndex, toRead).CopyTo(destination.Slice(totalRead));

                readIndex = (readIndex + toRead) % wrapBoundary;

                if (!AllowReadLooping)
                {
                    count -= toRead;
                }

                totalRead += toRead;

                remainingToFill -= toRead;
            }

            return totalRead;
        }

        public void Clear()
        {
            readIndex = 0;
            writeIndex = 0;
            count = 0;

            Array.Clear(array);
        }
    }



    /*using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using GeoLib.GeoUtils;

    namespace Toy_Synthesizer.Game.CommonUtils
    {
        public class ArrayRingBuffer<T>
        {
            private readonly int length;
            private readonly T[] array;

            private int readIndex;
            private int writeIndex;

            public int Length
            {
                get => length;
            }

            public int ReadIndex
            {
                get => readIndex;
            }

            public int WriteIndex
            {
                get => writeIndex;
            }

            public ArrayRingBuffer(int length)
            {
                this.length = length;
                array = new T[length];
            }

            // The returned span's length may not be count if that much was not available.
            public ReadOnlySpan<T> Read(int count)
            {
                if (ReadIndex == -1 || Length == 0)
                {
                    return ReadOnlySpan<T>.Empty;
                }

                int realCount = Math.Min(count, Length - ReadIndex);

                int readIndex = ReadIndex;

                if (readIndex + realCount >= Length)
                {
                    this.readIndex = -1;
                }
                else
                {
                    this.readIndex += realCount;
                }

                return new ReadOnlySpan<T>(array, ReadIndex, realCount);
            }

            public void Write(ReadOnlySpan<T> values)
            {
                if (values.Length > Length)
                {
                    throw new InvalidOperationException($"values was too large for this buffer.");
                }

                int currentLength = values.Length;

                while (currentLength != 0)
                {
                    int writeCount = currentLength - writeIndex;

                    ArrayUtils.ArrayCopy(values, array, sourceOffset: 0, destinationOffset: writeIndex, count: writeCount);

                    writeIndex = Math.Abs(Length - writeIndex + writeCount);

                    currentLength -= writeCount;
                }
            }
        }
    }
    */
}
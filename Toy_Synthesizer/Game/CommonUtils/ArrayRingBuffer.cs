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
}
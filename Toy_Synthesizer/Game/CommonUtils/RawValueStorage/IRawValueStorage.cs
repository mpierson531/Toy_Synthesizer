using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.CommonUtils.RawValueStorage
{
    public interface IRawValueStorage
    {
        static abstract int StorageSize { get; }

        ref ulong StorageAddress { get; }

        T Read<T>() where T : unmanaged;
        void Write<T>(T value) where T : unmanaged;

        void ReadBuffer<T>(Span<T> buffer, out int realCount) where T : unmanaged;
        void ReadBuffer<T>(Span<T> buffer, int sourceOffset, int destinationOffset, int requestedCount, out int realCount) where T : unmanaged;

        void WriteBuffer<T>(Span<T> buffer) where T : unmanaged;
        void WriteBuffer<T>(Span<T> buffer, int offset, int count) where T : unmanaged;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Toy_Synthesizer.Game.CommonUtils.RawValueStorage;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    // Should be used through DSPCommand, which has an AudioSource field, so this will not contain a target.
    public readonly struct AudioSourceCommand
    {
        public static AudioSourceCommand SendDouble(int commandID, double value)
        {
            return SendValue(commandID, value);
        }

        public static AudioSourceCommand SendFloat(int commandID, float value)
        {
            return SendValue(commandID, value);
        }

        public static AudioSourceCommand SendInt(int commandID, int value)
        {
            return SendValue(commandID, value);
        }

        public static AudioSourceCommand SendValue<T>(int commandID, T value) where T : unmanaged
        {
            if (ValueStorageUtils.SizeOf_unmanaged<T>() > RawValueStorage_64B.STORAGE_SIZE)
            {
                throw new InvalidOperationException("Size of value is too large.");
            }

            return Create(commandID: commandID, valueStorage: RawValueStorage_64B.From(value));
        }

        public static AudioSourceCommand SendObject(int commandID, object value)
        {
            return Create(commandID: commandID, objectValue: value);
        }

        public static AudioSourceCommand Create(int commandID,
                                                RawValueStorage_64B valueStorage = default,
                                                object objectValue = null,
                                                object objectValue2 = null)
        {
            return new AudioSourceCommand(commandID: commandID, 
                                          valueStorage: valueStorage,
                                          objectValue: objectValue,
                                          objectValue2: objectValue2);
        }

        public static void Validate(ref readonly AudioSourceCommand command)
        {

        }

        public readonly int CommandID;

        public readonly RawValueStorage_64B ValueStorage;

        public readonly object ObjectValue;
        public readonly object ObjectValue2;

        private AudioSourceCommand(int commandID,
                                   RawValueStorage_64B valueStorage = default,
                                   object objectValue = null,
                                   object objectValue2 = null)
        {
            CommandID = commandID;
            ValueStorage = valueStorage;
            ObjectValue = objectValue;
            ObjectValue2 = objectValue2;

            Validate(ref this);
        }
    }

}

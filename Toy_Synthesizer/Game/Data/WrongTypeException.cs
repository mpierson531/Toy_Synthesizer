using System;

namespace Toy_Synthesizer.Game.Data
{
    public class WrongTypeException<ExpectedType> : Exception
    {
        private WrongTypeException(object foundValue)
            : base($"Expected type of \"{typeof(ExpectedType).FullName}\", found type of \"{foundValue.GetType().FullName}\" with value of \"{Convert.ToString(foundValue)}\".")
        {

        }

        private WrongTypeException(Type foundType)
            : base($"Expected type of \"{typeof(ExpectedType).FullName}\", requested type was \"{foundType.FullName}\".")
        {

        }

        public static WrongTypeException<ExpectedType> WrongValue(object foundValue)
        {
            return new WrongTypeException<ExpectedType>(foundValue);
        }

        public static WrongTypeException<ExpectedType> WrongType(Type foundType)
        {
            return new WrongTypeException<ExpectedType>(foundType);
        }
    }
}

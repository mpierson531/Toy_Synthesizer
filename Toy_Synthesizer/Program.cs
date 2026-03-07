using System;

using GeoLib;

namespace Toy_Synthesizer
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using Geo geo = new Geo();

            geo.Screen = new Game.Game(geo);

            geo.Run();
        }
    }
}

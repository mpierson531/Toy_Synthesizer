using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoUtils;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.Filters
{
    public interface ISVFProvider
    {
        SVF GetFilter();
        void SetFilter(SVF filter);

        double GetFilterBaseCutoff();
        void SetFilterBaseCutoff(double baseCutoff);

        double GetFilterResonance();
        void SetFilterResonance(double resonance);

        double GetFilterGain();
        void SetFilterGain(double gain);

        ISVFMixProvider MixProvider { get; }
    }

    public interface ISVFMixProvider
    {
        UnmanagedNullable<SVFMix> GetMix();
        void SetMix(UnmanagedNullable<SVFMix> mix);

        double GetLowMix();
        void SetLowMix(double lowMix);

        double GetHighMix();
        void SetHighMix(double highMix);

        double GetBandMix();
        void SetBandMix(double bandMix);
    }
}

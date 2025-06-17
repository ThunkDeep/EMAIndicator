#region Using declarations
using System;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    // Custom lightweight VWAP calculation. Renamed from VWAP to avoid
    // conflicts with NinjaTrader's Order Flow VWAP indicator.
    public class SimpleVWAP : Indicator
    {
        private double cumulativePV = 0.0;
        private double cumulativeVolume = 0.0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Simple VWAP calculation.";
                Name = "SimpleVWAP";
                IsOverlay = true;
                AddPlot(Brushes.Goldenrod, "SimpleVWAP");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar == 0 || Bars.IsFirstBarOfSession)
            {
                cumulativePV = 0.0;
                cumulativeVolume = 0.0;
            }

            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3.0;
            double vol = Volume[0];
            cumulativePV += typicalPrice * vol;
            cumulativeVolume += vol;

            if (cumulativeVolume.ApproxCompare(0.0) == 0)
                Value[0] = typicalPrice;
            else
                Value[0] = cumulativePV / cumulativeVolume;
        }
    }
}

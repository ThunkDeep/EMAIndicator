#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class VWAP : Indicator
    {
        private double cumulativePV = 0.0;
        private double cumulativeVolume = 0.0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Simple VWAP calculation.";
                Name = "VWAP";
                IsOverlay = true;
                AddPlot(Brushes.Goldenrod, "VWAP");
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

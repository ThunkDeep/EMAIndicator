#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AdvancedEMATouchStrategy : Indicator
    {
        private EMA ema;
        private VOL volume;
        private SMA volumeAvg;
        private SimpleVWAP vwap;
        
        private List<double> swingHighs = new List<double>();
        private List<double> swingLows = new List<double>();
        private double lastHigh = 0;
        private double lastLow = double.MaxValue;
        private bool uptrend = true;
        
        // Multi-timeframe EMAs
        private EMA ema15min;
        private EMA ema60min;
        
        // Signal tracking
        private bool signalGenerated = false;
        private int signalBar = 0;
        
        // Confluence score
        private int confluenceScore = 0;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Advanced EMA Touch Strategy with Multiple Confluence Factors";
                Name = "AdvancedEMATouchStrategy";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Parameters
                EMAPeriod = 21;
                VolumeMultiplier = 1.5;
                SwingStrength = 5;
                SlopeThreshold = 0.0002;
                PullbackDepthShallow = 0.382;
                PullbackDepthDeep = 0.618;
                MinConfluenceScore = 5;
                ShowConfluenceLabels = true;
                AlertOnSignal = true;
                
                // Visual
                BullishSignalBrush = Brushes.Lime;
                BearishSignalBrush = Brushes.Red;
                EMABrush = Brushes.DodgerBlue;
                
                AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "EMA");
            }
            else if (State == State.Configure)
            {
                // Add additional data series for multi-timeframe analysis
                AddDataSeries(BarsPeriodType.Minute, 15);
                AddDataSeries(BarsPeriodType.Minute, 60);
            }
            else if (State == State.DataLoaded)
            {
                ema = EMA(EMAPeriod);
                volume = VOL();
                volumeAvg = SMA(volume, 20);
                vwap = SimpleVWAP();
                
                // Multi-timeframe EMAs
                ema15min = EMA(BarsArray[1], EMAPeriod);
                ema60min = EMA(BarsArray[2], EMAPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(EMAPeriod, SwingStrength * 2))
                return;
                
            // Only process primary series for main logic
            if (BarsInProgress == 0)
            {
                // Plot EMA
                Value[0] = ema[0];
                
                // Update trend structure
                UpdateTrendStructure();
                
                // Check for EMA touch and all confluence factors
                if (CheckEMATouchSetup())
                {
                    GenerateSignal();
                }
                
                // Draw confluence information
                if (ShowConfluenceLabels && signalGenerated && CurrentBar - signalBar < 3)
                {
                    DrawConfluenceInfo();
                }
            }
        }
        
        private void UpdateTrendStructure()
        {
            // Identify swing points
            double swingHigh = High[SwingStrength];
            double swingLow = Low[SwingStrength];
            bool isSwingHigh = true;
            bool isSwingLow = true;
            
            // Check for swing high
            for (int i = 1; i <= SwingStrength; i++)
            {
                if (High[SwingStrength - i] >= swingHigh || High[SwingStrength + i] >= swingHigh)
                {
                    isSwingHigh = false;
                    break;
                }
            }
            
            // Check for swing low
            for (int i = 1; i <= SwingStrength; i++)
            {
                if (Low[SwingStrength - i] <= swingLow || Low[SwingStrength + i] <= swingLow)
                {
                    isSwingLow = false;
                    break;
                }
            }
            
            // Update swing points lists
            if (isSwingHigh)
            {
                swingHighs.Add(swingHigh);
                if (swingHighs.Count > 10) swingHighs.RemoveAt(0);
                
                // Check for higher high
                if (swingHigh > lastHigh)
                {
                    if (!uptrend && swingLows.Count >= 2 && lastLow < swingLows[swingLows.Count - 2])
                        uptrend = true; // Trend reversal to uptrend
                }
                else if (uptrend && swingHigh < lastHigh)
                {
                    uptrend = false; // Potential trend break
                }
                lastHigh = swingHigh;
            }
            
            if (isSwingLow)
            {
                swingLows.Add(swingLow);
                if (swingLows.Count > 10) swingLows.RemoveAt(0);
                
                // Check for higher low
                if (swingLow > lastLow && uptrend)
                {
                    // Confirmed uptrend
                }
                else if (swingLow < lastLow && !uptrend)
                {
                    // Confirmed downtrend
                }
                lastLow = swingLow;
            }
        }
        
        private bool CheckEMATouchSetup()
        {
            confluenceScore = 0;
            
            // Reset signal tracking
            if (CurrentBar - signalBar > 5)
                signalGenerated = false;
            
            // 1. Check trend structure
            if (!uptrend) return false;
            confluenceScore++;
            
            // 2. Check EMA slope
            double emaSlope = (ema[0] - ema[3]) / ema[3];
            if (emaSlope < SlopeThreshold) return false;
            if (emaSlope > SlopeThreshold * 2) confluenceScore++;
            
            // 3. Check for touch and reclaim
            bool touchedEMA = Low[1] < ema[1] && Close[1] < ema[1];
            bool reclaimedEMA = Close[0] > ema[0] && Low[0] > ema[0] * 0.998;
            
            if (!touchedEMA || !reclaimedEMA) return false;
            confluenceScore++;
            
            // 4. Volume confirmation
            if (volume[0] > volumeAvg[0] * VolumeMultiplier)
            {
                confluenceScore += 2; // Strong signal
            }
            
            // 5. Wick behavior check
            double wickRatio = (High[0] - Close[0]) / (High[0] - Low[0]);
            if (wickRatio < 0.3 && Low[0] < ema[0])
            {
                confluenceScore++; // Rejection wick
            }
            
            // 6. Liquidity sweep check
            if (swingLows.Count >= 2 && Low[1] < swingLows[swingLows.Count - 2])
            {
                confluenceScore += 2; // Liquidity grab
            }
            
            // 7. Pullback depth analysis
            double pullbackDepth = (High[HighestBar(High, 20)] - Low[1]) / (High[HighestBar(High, 20)] - Low[LowestBar(Low, 20)]);
            if (pullbackDepth <= PullbackDepthShallow)
            {
                confluenceScore++; // Shallow pullback
            }
            else if (pullbackDepth >= PullbackDepthDeep)
            {
                confluenceScore--; // Deep pullback warning
            }
            
            // 8. Additional confluence checks
            // VWAP confluence
            if (Close[0] > vwap[0] && Low[1] > vwap[1] * 0.995)
                confluenceScore++;
                
            // Fibonacci retracement (simplified)
            double fib382 = lastHigh - (lastHigh - lastLow) * 0.382;
            if (Math.Abs(Low[1] - fib382) / fib382 < 0.01)
                confluenceScore++;
            
            // 9. Multi-timeframe agreement
            if (CurrentBars.Length > 1 && CurrentBars[1] >= EMAPeriod &&
                ema15min != null && ema15min.Count > 0 && Close[0] > ema15min[0])
                confluenceScore++;
            if (CurrentBars.Length > 2 && CurrentBars[2] >= EMAPeriod &&
                ema60min != null && ema60min.Count > 0 && Close[0] > ema60min[0])
                confluenceScore++;
            
            return confluenceScore >= MinConfluenceScore;
        }
        
        private void GenerateSignal()
        {
            signalGenerated = true;
            signalBar = CurrentBar;
            
            // Draw arrow
            Draw.ArrowUp(this, "Signal" + CurrentBar, true, 0, Low[0] - TickSize * 5, BullishSignalBrush);
            
            // Alert
            if (AlertOnSignal)
            {
                Alert("EMATouchSignal", Priority.High, "EMA Touch Buy Signal - Score: " + confluenceScore, 
                    NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 10, 
                    Brushes.White, BullishSignalBrush);
            }
        }
        
        private void DrawConfluenceInfo()
        {
            string confluenceText = "Score: " + confluenceScore + "\n";
            
            if (uptrend) confluenceText += "✓ Uptrend\n";
            
            double emaSlope = (ema[0] - ema[3]) / ema[3];
            if (emaSlope > SlopeThreshold) confluenceText += "✓ EMA Slope\n";
            
            if (volume[0] > volumeAvg[0] * VolumeMultiplier) confluenceText += "✓ Volume\n";
            
            if (Close[0] > vwap[0]) confluenceText += "✓ Above VWAP\n";
            
            if (CurrentBars.Length > 1 && CurrentBars[1] >= EMAPeriod &&
                ema15min != null && Close[0] > ema15min[0])
                confluenceText += "✓ 15m Bull\n";
            
            Draw.Text(this, "Confluence" + CurrentBar, confluenceText, 0, High[0] + TickSize * 10, 
                BullishSignalBrush);
        }
        
        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="EMA Period", Description="Period for EMA calculation", Order=1, GroupName="Parameters")]
        public int EMAPeriod { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name="Volume Multiplier", Description="Multiplier for volume spike detection", Order=2, GroupName="Parameters")]
        public double VolumeMultiplier { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Swing Strength", Description="Bars for swing point detection", Order=3, GroupName="Parameters")]
        public int SwingStrength { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name="Slope Threshold", Description="Minimum EMA slope for valid signal", Order=4, GroupName="Parameters")]
        public double SlopeThreshold { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name="Shallow Pullback", Description="Max depth for shallow pullback", Order=5, GroupName="Parameters")]
        public double PullbackDepthShallow { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name="Deep Pullback", Description="Min depth for deep pullback", Order=6, GroupName="Parameters")]
        public double PullbackDepthDeep { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name="Min Confluence Score", Description="Minimum score to trigger signal", Order=7, GroupName="Parameters")]
        public int MinConfluenceScore { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name="Show Confluence Labels", Description="Display confluence information", Order=8, GroupName="Display")]
        public bool ShowConfluenceLabels { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name="Alert on Signal", Description="Generate alerts for signals", Order=9, GroupName="Display")]
        public bool AlertOnSignal { get; set; }
        
        [XmlIgnore]
        [Display(Name="Bullish Signal", Description="Color for bullish signals", Order=10, GroupName="Colors")]
        public Brush BullishSignalBrush { get; set; }
        
        [XmlIgnore]
        [Display(Name="Bearish Signal", Description="Color for bearish signals", Order=11, GroupName="Colors")]
        public Brush BearishSignalBrush { get; set; }
        
        [XmlIgnore]
        [Display(Name="EMA Color", Description="Color for EMA line", Order=12, GroupName="Colors")]
        public Brush EMABrush { get; set; }
        
        [Browsable(false)]
        public string BullishSignalBrushSerializable
        {
            get { return Serialize.BrushToString(BullishSignalBrush); }
            set { BullishSignalBrush = Serialize.StringToBrush(value); }
        }
        
        [Browsable(false)]
        public string BearishSignalBrushSerializable
        {
            get { return Serialize.BrushToString(BearishSignalBrush); }
            set { BearishSignalBrush = Serialize.StringToBrush(value); }
        }
        
        [Browsable(false)]
        public string EMABrushSerializable
        {
            get { return Serialize.BrushToString(EMABrush); }
            set { EMABrush = Serialize.StringToBrush(value); }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private AdvancedEMATouchStrategy[] cacheAdvancedEMATouchStrategy;
        public AdvancedEMATouchStrategy AdvancedEMATouchStrategy(int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            return AdvancedEMATouchStrategy(Input, eMAPeriod, volumeMultiplier, swingStrength, slopeThreshold, pullbackDepthShallow, pullbackDepthDeep, minConfluenceScore, showConfluenceLabels, alertOnSignal);
        }

        public AdvancedEMATouchStrategy AdvancedEMATouchStrategy(ISeries<double> input, int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            if (cacheAdvancedEMATouchStrategy != null)
                for (int idx = 0; idx < cacheAdvancedEMATouchStrategy.Length; idx++)
                    if (cacheAdvancedEMATouchStrategy[idx] != null && cacheAdvancedEMATouchStrategy[idx].EMAPeriod == eMAPeriod && cacheAdvancedEMATouchStrategy[idx].VolumeMultiplier == volumeMultiplier && cacheAdvancedEMATouchStrategy[idx].SwingStrength == swingStrength && cacheAdvancedEMATouchStrategy[idx].SlopeThreshold == slopeThreshold && cacheAdvancedEMATouchStrategy[idx].PullbackDepthShallow == pullbackDepthShallow && cacheAdvancedEMATouchStrategy[idx].PullbackDepthDeep == pullbackDepthDeep && cacheAdvancedEMATouchStrategy[idx].MinConfluenceScore == minConfluenceScore && cacheAdvancedEMATouchStrategy[idx].ShowConfluenceLabels == showConfluenceLabels && cacheAdvancedEMATouchStrategy[idx].AlertOnSignal == alertOnSignal && cacheAdvancedEMATouchStrategy[idx].EqualsInput(input))
                        return cacheAdvancedEMATouchStrategy[idx];
            return CacheIndicator<AdvancedEMATouchStrategy>(new AdvancedEMATouchStrategy(){ EMAPeriod = eMAPeriod, VolumeMultiplier = volumeMultiplier, SwingStrength = swingStrength, SlopeThreshold = slopeThreshold, PullbackDepthShallow = pullbackDepthShallow, PullbackDepthDeep = pullbackDepthDeep, MinConfluenceScore = minConfluenceScore, ShowConfluenceLabels = showConfluenceLabels, AlertOnSignal = alertOnSignal }, input, ref cacheAdvancedEMATouchStrategy);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.AdvancedEMATouchStrategy AdvancedEMATouchStrategy(int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            return indicator.AdvancedEMATouchStrategy(Input, eMAPeriod, volumeMultiplier, swingStrength, slopeThreshold, pullbackDepthShallow, pullbackDepthDeep, minConfluenceScore, showConfluenceLabels, alertOnSignal);
        }

        public Indicators.AdvancedEMATouchStrategy AdvancedEMATouchStrategy(ISeries<double> input , int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            return indicator.AdvancedEMATouchStrategy(input, eMAPeriod, volumeMultiplier, swingStrength, slopeThreshold, pullbackDepthShallow, pullbackDepthDeep, minConfluenceScore, showConfluenceLabels, alertOnSignal);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.AdvancedEMATouchStrategy AdvancedEMATouchStrategy(int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            return indicator.AdvancedEMATouchStrategy(Input, eMAPeriod, volumeMultiplier, swingStrength, slopeThreshold, pullbackDepthShallow, pullbackDepthDeep, minConfluenceScore, showConfluenceLabels, alertOnSignal);
        }

        public Indicators.AdvancedEMATouchStrategy AdvancedEMATouchStrategy(ISeries<double> input , int eMAPeriod, double volumeMultiplier, int swingStrength, double slopeThreshold, double pullbackDepthShallow, double pullbackDepthDeep, int minConfluenceScore, bool showConfluenceLabels, bool alertOnSignal)
        {
            return indicator.AdvancedEMATouchStrategy(input, eMAPeriod, volumeMultiplier, swingStrength, slopeThreshold, pullbackDepthShallow, pullbackDepthDeep, minConfluenceScore, showConfluenceLabels, alertOnSignal);
        }
    }
}

#endregion

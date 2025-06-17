namespace NinjaTrader.NinjaScript
{
    public static class DoubleExtensions
    {
        /// <summary>
        /// Compare two double values with tolerance.
        /// Returns 0 if difference <= tolerance, -1 if value < other, 1 otherwise.
        /// </summary>
        public static int ApproxCompare(this double value, double other, double tolerance = 1e-10)
        {
            double diff = value - other;
            if (System.Math.Abs(diff) <= tolerance)
                return 0;
            return diff > 0 ? 1 : -1;
        }
    }
}

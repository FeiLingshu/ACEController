using System;
using System.Diagnostics;

namespace ACEController.Experimental
{
    public static class Ex
    {
        private static readonly double[] _lastWorkingSetBytes = new double[3] { 0D, 0D, 0D };
        private const int PageSize = 4096;
        private const float MinReadAheadFactor = 1.0F;
        private const float MaxReadAheadFactor = 4.0F;
        public static double RunMath(PerformanceCounter P_PageFault, PerformanceCounter P_WorkingSet, byte index)
        {
            byte _ = 0;
            _ = index switch
            {
                1 => index,
                2 => index,
                _ => 0,
            };
            if (_lastWorkingSetBytes[_] == 0F)
            {
                _lastWorkingSetBytes[_] = P_WorkingSet.NextValue();
                return 0F;
            }
            double PageFault = (double)P_PageFault.NextValue();
            double WorkingSet = (double)P_WorkingSet.NextValue();
            double WorkingSetDleta = WorkingSet - _lastWorkingSetBytes[_];
            _lastWorkingSetBytes[_] = WorkingSetDleta;
            double minAccessBytes = PageFault * PageSize;
            double readAheadFactor = Math.Max(1.0D,
                WorkingSetDleta > 0 ? WorkingSetDleta / minAccessBytes : 1.0D);
            readAheadFactor = Math.Max(MinReadAheadFactor, Math.Min(MaxReadAheadFactor, readAheadFactor));
            double rawEstimatedBytes = minAccessBytes * readAheadFactor;
            return Math.Max(0, rawEstimatedBytes);
        }

        private const double To_K = 1;
        private const double To_M = 1024;
        private const double To_G = 1024 * 1024;
        private const double MaxSize = 1024 * 1024 * 9999.99D;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:不需要赋值", Justification = "<挂起>")]
        public static string GetSpeed(double source_1, double source_2)
        {
            source_1 /= 1024;
            source_2 /= 1024;
            double source = source_1 + source_2;
            if (source < 0) source = 0;
            if (source > MaxSize) source = MaxSize;
            string unit = string.Empty;
            if (source >= To_G)
            {
                source /= To_G;
                unit = "GB/s";
            }
            else if (source >= To_M)
            {
                source /= To_M;
                unit = "MB/s";
            }
            else
            {
                source /= To_K;
                unit = "KB/s";
            }
            return $"{Math.Round(source, 2, MidpointRounding.AwayFromZero),7:F2}{unit}";
        }
    }
}

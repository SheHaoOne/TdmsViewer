namespace NvhLibCSharp.Utils
{

    public static class MathUtils
    {
        public static double ConvertIncrementToOverlap(double deltaTime, double frameLength, double increment)
        {
            var step = Math.Floor(increment / deltaTime);
            var overlap = 1 - step / frameLength;

            return overlap;
        }

        public static IEnumerable<double> Linspace(double start, double end, int count)
        {
            double delta = (end - start) / (count - 1);
            return Enumerable.Range(0, count).Select(i => start + delta * i);
        }

        public static IEnumerable<double> Logspace(double start, double end, int count)
        {
            double delta = (end - start) / (count - 1);
            return Enumerable.Range(0, count).Select(i => Math.Pow(10, start + delta * i)); 
        }
    }
}

using System.Numerics;
using System.Text.RegularExpressions;

namespace NvhLibCSharp.Utils
{
    public static class LoadData
    {
        public static double[] Double(string filePath) =>
            File.ReadAllLines(filePath)
            .Where(x => !x.StartsWith('#') && x.Length >= 1)
            .Select(double.Parse)
            .ToArray();

        public static Complex[] Complex(string filePath) =>
            File.ReadAllLines(filePath)
            .Where(x => !x.StartsWith('#') && x.Length > 1)
            .Select(x => ComplexParser.Parse(x))
            .ToArray();

        public static double[] Double(string filePath, out double[] timeVector, out double deltaTime)
        {
            var sample = new List<double>();
            var timeVectorList = new List<double>();

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith('#') || trimmedLine.Length < 1)
                    continue;

                var splited = trimmedLine.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                timeVectorList.Add(double.Parse(splited[0]));
                sample.Add(double.Parse(splited[1]));
            }

            timeVector = timeVectorList.ToArray();
            deltaTime = timeVector[1] - timeVector[0];

            return sample.ToArray();
        }
    }

    public static partial class ComplexParser
    {
        private static readonly Regex ComplexPattern = PythonExplexRegex();

        public static Complex Parse(string input)
        {
            var match = ComplexPattern.Match(input);
            if (!match.Success)
                throw new FormatException($"Invalid complex format: {input}");

            double real = double.Parse(match.Groups["real"].Value, System.Globalization.NumberStyles.Float);
            double imag = double.Parse(match.Groups["imag"].Value, System.Globalization.NumberStyles.Float);

            if (match.Groups["sign"].Value == "-")
                imag = -imag;

            return new Complex(real, imag);
        }

        [GeneratedRegex(@"^\s*\(?\s*(?<real>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*(?<sign>[+-])\s*(?<imag>\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)j\s*\)?\s*$", RegexOptions.Compiled)]
        private static partial Regex PythonExplexRegex();
    }
}

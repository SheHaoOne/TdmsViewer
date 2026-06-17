using NvhLibCSharp;
using NvhLibCSharp.Enums;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

internal static class NvhEnumHelper
{
    public static Window ParseWindow(string value) =>
        Enum.TryParse<Window>(value, true, out var parsed) ? parsed : Window.Hanning;

    public static Weight ParseWeight(string value) =>
        Enum.TryParse<Weight>(value, true, out var parsed) ? parsed : Weight.A;

    public static Scale ParseScale(string value) =>
        Enum.TryParse<Scale>(value, true, out var parsed) ? parsed : Scale.Db;

    public static Format ParseFormat(string value) =>
        Enum.TryParse<Format>(value, true, out var parsed) ? parsed : Format.Rms;

    public static Average ParseAverage(string value) =>
        Enum.TryParse<Average>(value, true, out var parsed) ? parsed : Average.Energy;

    public static SpectraCalcType ParseCalcType(string value) =>
        Enum.TryParse<SpectraCalcType>(value, true, out var parsed) ? parsed : SpectraCalcType.Resolution;

    public static SpectraStepType ParseStepType(string value) =>
        Enum.TryParse<SpectraStepType>(value, true, out var parsed) ? parsed : SpectraStepType.Increment;

    public static Octave ParseOctave(string value) =>
        Enum.TryParse<Octave>(value, true, out var parsed) ? parsed : Octave.ThirdOctave;
}

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

    public static RpmTrigger ParseRpmTrigger(string value) =>
        Enum.TryParse<RpmTrigger>(value, true, out var parsed) ? parsed : RpmTrigger.Up;

    public static SoundField ParseSoundField(string value) =>
        Enum.TryParse<SoundField>(value, true, out var parsed) ? parsed : SoundField.Free;

    public static SharpnessWeighting ParseSharpnessWeighting(string value) =>
        Enum.TryParse<SharpnessWeighting>(value, true, out var parsed) ? parsed : SharpnessWeighting.Din;

    public static FluctuationMethod ParseFluctuationMethod(string value) =>
        Enum.TryParse<FluctuationMethod>(value, true, out var parsed) ? parsed : FluctuationMethod.Stationary;

    public static FractionalResamplerPlanningMode ParseResamplerPlanning(string value) =>
        Enum.TryParse<FractionalResamplerPlanningMode>(value, true, out var parsed)
            ? parsed
            : FractionalResamplerPlanningMode.Balanced;
}

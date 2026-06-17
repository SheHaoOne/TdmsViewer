namespace TdmsViewer.Controls;

public sealed class WaveformTimeRangeSelectedEventArgs : EventArgs
{
    public WaveformTimeRangeSelectedEventArgs(double startSec, double endSec)
    {
        StartSec = startSec;
        EndSec = endSec;
    }

    public double StartSec { get; }

    public double EndSec { get; }
}

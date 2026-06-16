using System.Runtime.InteropServices;

namespace NvhLibCSharp.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Signal(double[] samples, double deltaTime, long unixTime = 0)
    {
        public IntPtr Samples = samples.ToIntPtr(out _);
        public int Length = samples.Length;
        public double DeltaTime = deltaTime;
        public long UnixTime = unixTime;
    }
}

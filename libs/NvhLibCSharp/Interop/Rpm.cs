using System.Runtime.InteropServices;

namespace NvhLibCSharp.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rpm(double[] rpmValues, double increment, double startOffset = 0)
    {
        public IntPtr RpmValues = rpmValues.ToIntPtr(out _);
        public IntPtr TimeValues = Enumerable.Range(0, rpmValues.Length).Select(i => startOffset + i * increment).ToArray().ToIntPtr(out _);
        public int Length = rpmValues.Length;
    }
}

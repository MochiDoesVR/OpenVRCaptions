using System;
using System.Numerics;
using Valve.VR;

namespace OpenVRCaptions.Renderer
{
    public static class Utilities
    {
        public static HmdMatrix34_t ToHmdMatrix34_t(this Matrix4x4 matrix)
        {
            return new HmdMatrix34_t()
            {
                m0 = matrix.M11,
                m1 = matrix.M12,
                m2 = matrix.M13,
                m3 = matrix.M41,
                m4 = matrix.M21,
                m5 = matrix.M22,
                m6 = matrix.M23,
                m7 = matrix.M42,
                m8 = matrix.M31,
                m9 = matrix.M32,
                m10 = matrix.M33,
                m11 = matrix.M43,
            };
        }

        public static Matrix4x4 ToMatrix4x4(this HmdMatrix34_t matrix)
        {
            return new Matrix4x4(
                matrix.m0, matrix.m1, matrix.m2, 0,
                matrix.m4, matrix.m5, matrix.m6, 0,
                matrix.m8, matrix.m9, matrix.m10, 0,
                matrix.m3, matrix.m7, matrix.m11, 1
            );
        }
        
        public static float ToRadians(this float val)
        {
            return (float)((Math.PI / 180) * val);
        }
        
        public static float ToDegrees(this float val)
        {
            return (float)((180 / Math.PI) * val);
        }
    }
}
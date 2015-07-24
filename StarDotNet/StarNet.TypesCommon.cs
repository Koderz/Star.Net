#region License

// Copyright (c) 2015 Koderz ( http://koderz.me/ )
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#endregion License

#pragma warning disable 1591
using System;
using System.Text.RegularExpressions;

namespace StarDotNet
{
    internal static class StarNetHelpersCommon
    {
        // These are the options to use for all regular expressions in the StarNet helpers.
        public static readonly RegexOptions InternalRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        // Admin command failed: Error packing parameters
        public static Regex AdminCommandFailed = new Regex(@"Admin command failed: Error packing parameters", InternalRegexOptions);

        public static void ThrowUnexpectedResponseIfNotSuccess(Match match, string expected, string got)
        {
            if (!match.Success)
                throw new InvalidOperationException("Expected: " + expected + " | Got: " + got);
        }

        public static bool IsStringNull(this string value)
        {
            if (value == null)
                return true;

            string trimmed = value.Trim();

            return trimmed.Equals(string.Empty, StringComparison.InvariantCultureIgnoreCase) ||
                   trimmed.Equals("null", StringComparison.InvariantCultureIgnoreCase);
        }

        public static string ClearStringNull(this string value)
        {
            if (value == null)
                return null;

            string trimmed = value.Trim();

            if (trimmed.Equals(string.Empty, StringComparison.InvariantCultureIgnoreCase) ||
                trimmed.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                return null;

            return value;
        }

        // Regex parts
        // (?<playerName>.*)
        // (?<accountName>.*)
        // (?<factionId>.*)
        // (?<ipAddress>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})
        // (?<xPos>[0-9\.\-E\+]*), (?<yPos>[0-9\.\-E\+]*), (?<zPos>[0-9\.\-E\+]*)
        // (?<xSector>[0-9\-]*), (?<ySector>[0-9\-]*), (?<zSector>[0-9\-]*)
    }

    [Serializable]
    public class AdminCommandFailedException : Exception
    {
        public AdminCommandFailedException(string command)
            : base(command)
        {
        }
    }

    [Serializable]
    public struct Vector3<T>
    {
        public T X;
        public T Y;
        public T Z;

        public Vector3(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return "[" + X + ", " + Y + ", " + Z + "]";
        }
    }

    [Serializable]
    public struct Vector4<T>
    {
        public T X;
        public T Y;
        public T Z;
        public T W;

        public Vector4(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return "[" + X + ", " + Y + ", " + Z + ", " + W + "]";
        }
    }
}
#pragma warning restore 1591
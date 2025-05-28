#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;

namespace Newtonsoft.Json.Utilities
{
    internal readonly struct ByteStringReference
    {
        /// <summary>
        /// Question mark replaces any non-ASCII byte
        /// </summary>
        public const char UnicodeReplacementChar = '\uFFFD';

        private readonly byte[] _chars;
        private readonly int _startIndex;
        private readonly int _length;

        public byte this[int i] => _chars[i];

        public byte[] Chars => _chars;

        /// <summary>
        /// Start index of the actual data block
        /// </summary>
        public int StartIndex => _startIndex;

        /// <summary>
        /// Length of actual data block.
        /// Internal array '_chars' must contain at least (StartIndex + Length) elements.
        /// </summary>
        public int Length => _length;

        public ByteStringReference(byte[] chars, int startIndex, int length)
        {
            _chars = chars;
            _startIndex = startIndex;
            _length = length;
        }

        [Obsolete("This method is relatively slow as it copies characters one-by-one")]
        public ByteStringReference(char[] chars, int startIndex, int length)
        {
            _chars = new byte[chars.Length];
            _startIndex = startIndex;
            _length = length;

            for (int j = 0; j < chars.Length; j++)
            {
                char ch = chars[j];
                // https://www.ascii-code.com/
                if (ch < 128)
                {
                    _chars[j] = (byte)ch;
                }
                else
                {
                    // Non ASCII character was detected
                    throw new ArgumentOutOfRangeException(nameof(ch),
                        "Non-ASCII character was detected. Code:" + (int)ch +
                        "; Hex:0x" + ((int)ch).ToString("X") +
                        "; char:" + ch);
                }
            }
        }

        /// <inhertitdoc/>
        public override string ToString()
        {
            //string res = System.Text.Encoding.ASCII.GetString(_chars, _startIndex, _length);
            //return res;
            //return new string(_chars, _startIndex, _length);

            char[] buf = new char[_length];
            for (int j = 0; j < _length; j++)
            {
                byte b = _chars[_startIndex + j];
                // https://www.ascii-code.com/
                if (b < 128)
                {
                    buf[j] = (char)b;
                }
                else
                {
                    // Non ASCII character was detected
                    //throw new ArgumentOutOfRangeException(nameof(b),
                    //    "Non-ASCII character was detected. Code:" + b +
                    //    "; Hex:0x" + b.ToString("X") +
                    //    "; char:" + (char)b);
                    buf[j] = UnicodeReplacementChar;
                }
            }

            string res = new string(buf);
            return res;
        }
    }

    internal static class ByteStringReferenceExtensions
    {
        public static int IndexOf(this ByteStringReference s, char c, int startIndex, int length)
        {
            // https://www.ascii-code.com/
            if (c > 127)
            {
                // Non ASCII character was detected
                throw new ArgumentOutOfRangeException(nameof(c),
                    "Non-ASCII character was detected. Code:" + (int)c +
                    "; Hex:0x" + ((int)c).ToString("X") +
                    "; char:" + c);
            }

            int index = Array.IndexOf(s.Chars, (byte)c, s.StartIndex + startIndex, length);
            if (index == -1)
            {
                return -1;
            }

            return index - s.StartIndex;
        }

        public static int IndexOf(this ByteStringReference s, byte b, int startIndex, int length)
        {
            // https://www.ascii-code.com/
            if (b > 127)
            {
                // Non ASCII character was detected
                throw new ArgumentOutOfRangeException(nameof(b),
                    "Non-ASCII character was detected. Code:" + b +
                    "; Hex:0x" + b.ToString("X") +
                    "; char:" + (char)b);
            }

            int index = Array.IndexOf(s.Chars, b, s.StartIndex + startIndex, length);
            if (index == -1)
            {
                return -1;
            }

            return index - s.StartIndex;
        }

        public static bool StartsWith(this ByteStringReference s, string text)
        {
            if (text.Length > s.Length)
            {
                return false;
            }

            byte[] chars = s.Chars;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != chars[i + s.StartIndex])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool EndsWith(this ByteStringReference s, string text)
        {
            if (text.Length > s.Length)
            {
                return false;
            }

            byte[] chars = s.Chars;

            int start = s.StartIndex + s.Length - text.Length;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != chars[i + start])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

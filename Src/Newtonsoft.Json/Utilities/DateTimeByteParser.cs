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
    internal struct DateTimeByteParser
    {
        static DateTimeByteParser()
        {
            Power10 = new[] { -1, 10, 100, 1000, 10000, 100000, 1000000 };

            Lzyyyy = "yyyy".Length;
            Lzyyyy_ = "yyyy-".Length;
            Lzyyyy_MM = "yyyy-MM".Length;
            Lzyyyy_MM_ = "yyyy-MM-".Length;
            Lzyyyy_MM_dd = "yyyy-MM-dd".Length;
            Lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;
            LzHH = "HH".Length;
            LzHH_ = "HH:".Length;
            LzHH_mm = "HH:mm".Length;
            LzHH_mm_ = "HH:mm:".Length;
            LzHH_mm_ss = "HH:mm:ss".Length;
            Lz_ = "-".Length;
            Lz_zz = "-zz".Length;
        }

        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minute;
        public int Second;
        public int Fraction;
        public int ZoneHour;
        public int ZoneMinute;
        public ParserTimeZone Zone;

        private byte[] _byteText;
        private int _end;

        private static readonly int[] Power10;

        private static readonly int Lzyyyy;
        private static readonly int Lzyyyy_;
        private static readonly int Lzyyyy_MM;
        private static readonly int Lzyyyy_MM_;
        private static readonly int Lzyyyy_MM_dd;
        private static readonly int Lzyyyy_MM_ddT;
        private static readonly int LzHH;
        private static readonly int LzHH_;
        private static readonly int LzHH_mm;
        private static readonly int LzHH_mm_;
        private static readonly int LzHH_mm_ss;
        private static readonly int Lz_;
        private static readonly int Lz_zz;

        private const short MaxFractionDigits = 7;

        public bool Parse(byte[] byteText, int startIndex, int length)
        {
            _byteText = byteText;
            _end = startIndex + length;


            if (ParseDate_byte(startIndex) &&
                ParseCharAndCheck_byte(Lzyyyy_MM_dd + startIndex, 'T') &&
                ParseTimeAndZoneAndWhitespace_byte(Lzyyyy_MM_ddT + startIndex))
            {
                return true;
            }

            return false;
        }

        private bool ParseDate_byte(int start)
        {
            return (Parse4Digit_byte(start, out Year)
                    && 1 <= Year
                    && ParseCharAndCheck_byte(start + Lzyyyy, '-')
                    && Parse2Digit_byte(start + Lzyyyy_, out Month)
                    && 1 <= Month
                    && Month <= 12
                    && ParseCharAndCheck_byte(start + Lzyyyy_MM, '-')
                    && Parse2Digit_byte(start + Lzyyyy_MM_, out Day)
                    && 1 <= Day
                    && Day <= DateTime.DaysInMonth(Year, Month));
        }

        private bool ParseTimeAndZoneAndWhitespace_byte(int start)
        {
            return (ParseTime_byte(ref start) && ParseZone_byte(start));
        }

        private bool ParseTime_byte(ref int start)
        {
            if (!(Parse2Digit_byte(start, out Hour)
                  && Hour <= 24
                  && ParseCharAndCheck_byte(start + LzHH, ':')
                  && Parse2Digit_byte(start + LzHH_, out Minute)
                  && Minute < 60
                  && ParseCharAndCheck_byte(start + LzHH_mm, ':')
                  && Parse2Digit_byte(start + LzHH_mm_, out Second)
                  && Second < 60
                  && (Hour != 24 || (Minute == 0 && Second == 0)))) // hour can be 24 if minute/second is zero)
            {
                return false;
            }

            start += LzHH_mm_ss;
            if (ParseCharAndCheck_byte(start, '.'))
            {
                Fraction = 0;
                int numberOfDigits = 0;

                while (++start < _end && numberOfDigits < MaxFractionDigits)
                {
                    int digit = _byteText[start] - '0';
                    if (digit < 0 || digit > 9)
                    {
                        break;
                    }

                    Fraction = (Fraction * 10) + digit;

                    numberOfDigits++;
                }

                if (numberOfDigits < MaxFractionDigits)
                {
                    if (numberOfDigits == 0)
                    {
                        return false;
                    }

                    Fraction *= Power10[MaxFractionDigits - numberOfDigits];
                }

                if (Hour == 24 && Fraction != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool ParseZone_byte(int start)
        {
            if (start < _end)
            {
                char ch = (char)_byteText[start];
                if (ch == 'Z' || ch == 'z')
                {
                    Zone = ParserTimeZone.Utc;
                    start++;
                }
                else
                {
                    if (start + 2 < _end
                        && Parse2Digit_byte(start + Lz_, out ZoneHour)
                        && ZoneHour <= 99)
                    {
                        switch (ch)
                        {
                            case '-':
                                Zone = ParserTimeZone.LocalWestOfUtc;
                                start += Lz_zz;
                                break;

                            case '+':
                                Zone = ParserTimeZone.LocalEastOfUtc;
                                start += Lz_zz;
                                break;
                        }
                    }

                    if (start < _end)
                    {
                        if (ParseCharAndCheck_byte(start, ':'))
                        {
                            start += 1;

                            if (start + 1 < _end
                                && Parse2Digit_byte(start, out ZoneMinute)
                                && ZoneMinute <= 99)
                            {
                                start += 2;
                            }
                        }
                        else
                        {
                            if (start + 1 < _end
                                && Parse2Digit_byte(start, out ZoneMinute)
                                && ZoneMinute <= 99)
                            {
                                start += 2;
                            }
                        }
                    }
                }
            }

            return (start == _end);
        }

        private bool Parse4Digit_byte(int start, out int num)
        {
            if (start + 3 < _end)
            {
                int digit1 = _byteText[start] - '0';
                int digit2 = _byteText[start + 1] - '0';
                int digit3 = _byteText[start + 2] - '0';
                int digit4 = _byteText[start + 3] - '0';
                if ((0 <= digit1 && digit1 < 10) &&
                    (0 <= digit2 && digit2 < 10) &&
                    (0 <= digit3 && digit3 < 10) &&
                    (0 <= digit4 && digit4 < 10))
                {
                    // If all 4 values are valid DECIMAL DIGITS, then we are happy
                    num = (((((digit1 * 10) + digit2) * 10) + digit3) * 10) + digit4;
                    return true;
                }
            }
            num = 0;
            return false;
        }

        private bool Parse2Digit_byte(int start, out int num)
        {
            if (start + 1 < _end)
            {
                int digit1 = _byteText[start] - '0';
                int digit2 = _byteText[start + 1] - '0';
                if ((0 <= digit1 && digit1 < 10) &&
                    (0 <= digit2 && digit2 < 10))
                {
                    // If both values are valid DECIMAL DIGITS, then we are happy
                    num = (digit1 * 10) + digit2;
                    return true;
                }
            }
            num = 0;
            return false;
        }

        private bool ParseCharAndCheck_byte(int start, char ch)
        {
            return (start < _end && _byteText[start] == ch);
        }
    }
}

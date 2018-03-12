using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    internal static class Utility
    {
        public const string StringTab = "\t";

        public const char CharTab = '\t';

        public const string StringWhiteSpace = " ";
        public const char CharWhiteSpace = ' ';
        public const char CharNewLine = '\n';
        public const string StringNewLine = "\n";
        
        public static double Pow<T>(this T x, int exp) where T : IConvertible
        {
            double result = 0;
            double x2 = x.ToDouble(null);
            if (exp == 0) {
                result = 1;
            } else if (exp < 0) {
                result = Math.Pow(x2, exp);
            } else {
                result = 1.0;
                while (exp > 0) {
                    if (exp % 2 == 1) {
                        result *= x2;
                    }
                    exp >>= 1;
                    x2 *= x2;
                }
            }

            return result;
        }

        public static string ToText(this LogLevel level)
        {
            switch (level) {
                case LogLevel.Debug:
                    return "Debug";

                default:
                case LogLevel.Info:
                    return "Info";

                case LogLevel.Warning:
                    return "Warn";

                case LogLevel.Error:
                    return "Error";
            }
        }

        public static string GetString(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

        public static byte[] GetBytes(this string s) => Encoding.UTF8.GetBytes(s);

        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);

        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);

        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        public static bool In<T>(this T value, T arg1) => value.Equals(arg1);

        public static bool In<T>(this T value, T arg1, T arg2) => value.Equals(arg1) || value.Equals(arg2);

        public static bool In<T>(this T value, T arg1, T arg2, T arg3) => value.Equals(arg1) || value.Equals(arg2) || value.Equals(arg3);

        public static bool In<T>(this T value, params T[] args) => args.Any(x => x.Equals(value));

        public static T Max<T>(T val1, T val2) where T : IComparable<T> => val1.CompareTo(val2) == 1 ? val1 : val2;

        public static T Min<T>(T val1, T val2) where T : IComparable<T> => val1.CompareTo(val2) == -1 ? val1 : val2;

        public static T Between<T>(this T value, T min, T max) where T : IComparable<T> => Min(Max(value, min), max);

        public static void DequeueTo<T>(this Queue<T> collection, int count)
        {
            while (collection.Count > count) { collection.Dequeue(); }
        }

        public static string GetDateString(this DateTime date) => date.ToString("yyyy-MM-dd");

        public static string GetDateTimeString(this DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss");

        public static string GetTimeString(this DateTime date) => date.ToString("HH:mm:ss");

        public static string GetDateTimeStringForFileName(this DateTime date) => date.ToString("yyyy-MM-dd_HH-mm-ss");

        public static bool IsPortAvailabled(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var activeListenerList = new List<IPEndPoint>();

            activeListenerList.AddRange(ipGlobalProperties.GetActiveTcpConnections().Select(x => x.LocalEndPoint));
            activeListenerList.AddRange(ipGlobalProperties.GetActiveTcpListeners());
            activeListenerList.AddRange(ipGlobalProperties.GetActiveUdpListeners());

            return activeListenerList.All(x => x.Port != port);
        }

        public static IEnumerable<int> GetSequence(int from, int to, int step = 1)
        {
            for (int i = from; i <= to; i += step) {
                yield return i;
            }
        }

        public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source, Random rnd) => source.OrderBy(x => rnd.Next());

        public static T[] GetShuffle<T>(this IEnumerable<T> source, int seed)
        {
            Random rnd = new Random(seed);
            IEnumerable<T> shuffle = Shuffle(source, rnd);
            return shuffle.ToArray();
        }

        public static int[] GetShuffle(int size, int seed)
        {
            var source = GetSequence(0, size - 1, 1);

            return source.GetShuffle(seed);
        }

        private static readonly string[] unit = { "B", "KB", "MB", "GB" };

        public static string GetSizeString(long size)
        {
            int indexOfUnit = 0;
            double num = 0.0f;

            for (int i = 0; i < unit.Length; i++) {
                double s = 1024.Pow(i);
                if (size >= s) {
                    num = size / s;
                    indexOfUnit = i;
                } else {
                    break;
                }
            }

            return $"{num:0.00} {unit[indexOfUnit]}";
        }

        private static readonly DateTime TimeStampDate = new DateTime(1970, 1, 1);

        public static long GetTimeStamp(this DateTime date) => (date - TimeStampDate).Ticks;

        public static long GetTimeStamp() => GetTimeStamp(DateTime.UtcNow);

        public static string ParseValue(string text, string match)
        {
            if (text.IsNullOrWhiteSpace() || match.IsNullOrWhiteSpace()) {
                return string.Empty;
            }
            int start = text.IndexOf(match) + match.Length;

            int end = text.IndexOf(CharWhiteSpace, start);
            end = end == -1 ? text.Length : end;

            return text.Substring(start, end - start);
        }
    }
}
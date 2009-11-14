//
// DurationStatusFormatters.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using System.Collections.Generic;
using Mono.Unix;

using Hyena;

namespace Banshee.Sources
{
    public delegate void DurationStatusFormatHandler (StringBuilder builder, TimeSpan span);

    public class DurationStatusFormatters : List<DurationStatusFormatHandler>
    {
        private static StringBuilder default_builder = new StringBuilder ();

        public DurationStatusFormatters ()
        {
            Add (AwesomeConciseFormatter);
            Add (AnnoyingPreciseFormatter);
            Add (ConfusingPreciseFormatter);
        }

        public static string AwesomeConciseFormatter (TimeSpan span)
        {
            lock (default_builder) {
                default_builder.Remove (0, default_builder.Length);
                AwesomeConciseFormatter (default_builder, span);
                return default_builder.ToString ();
            }
        }

        public static void AwesomeConciseFormatter (StringBuilder builder, TimeSpan span)
        {
            if (span.Days > 0) {
                double days = span.Days + (span.Hours / 24.0);
                builder.AppendFormat (Catalog.GetPluralString ("{0} day", "{0} days",
                    StringUtil.DoubleToPluralInt (days)), StringUtil.DoubleToTenthsPrecision (days));
            } else if (span.Hours > 0) {
                double hours = span.Hours + (span.Minutes / 60.0);
                builder.AppendFormat (Catalog.GetPluralString ("{0} hour", "{0} hours",
                    StringUtil.DoubleToPluralInt (hours)), StringUtil.DoubleToTenthsPrecision (hours));
            } else {
                double minutes = span.Minutes + (span.Seconds / 60.0);
                builder.AppendFormat (Catalog.GetPluralString ("{0} minute", "{0} minutes",
                    StringUtil.DoubleToPluralInt (minutes)), StringUtil.DoubleToTenthsPrecision (minutes));
            }
        }

        public static string AnnoyingPreciseFormatter (TimeSpan span)
        {
            lock (default_builder) {
                default_builder.Remove (0, default_builder.Length);
                AnnoyingPreciseFormatter (default_builder, span);
                return default_builder.ToString ();
            }
        }

        public static void AnnoyingPreciseFormatter (StringBuilder builder, TimeSpan span)
        {
            if (span.Days > 0) {
                builder.AppendFormat (Catalog.GetPluralString ("{0} day", "{0} days", span.Days), span.Days);
                builder.Append (", ");
            }

            if (span.Hours > 0) {
                builder.AppendFormat (Catalog.GetPluralString ("{0} hour", "{0} hours", span.Hours), span.Hours);
                builder.Append (", ");
            }

            builder.AppendFormat (Catalog.GetPluralString ("{0} minute", "{0} minutes", span.Minutes), span.Minutes);
            builder.Append (", ");
            builder.AppendFormat (Catalog.GetPluralString ("{0} second", "{0} seconds", span.Seconds), span.Seconds);
        }

        public static string ConfusingPreciseFormatter (TimeSpan span)
        {
            lock (default_builder) {
                default_builder.Remove (0, default_builder.Length);
                ConfusingPreciseFormatter (default_builder, span);
                return default_builder.ToString ();
            }
        }

        public static void ConfusingPreciseFormatter (StringBuilder builder, TimeSpan span)
        {
            if (span.Days > 0) {
                builder.AppendFormat ("{0}:{1:00}:", span.Days, span.Hours);
            } else if (span.Hours > 0) {
                builder.AppendFormat ("{0}:", span.Hours);
            }

            if (span.TotalHours < 1 || span.TotalMinutes < 1) {
                builder.AppendFormat ("{0}:{1:00}", span.Minutes, span.Seconds);
            } else {
                builder.AppendFormat ("{0:00}:{1:00}", span.Minutes, span.Seconds);
            }
        }

        public static string ApproximateVerboseFormatter (TimeSpan span)
        {
            lock (default_builder) {
                default_builder.Remove (0, default_builder.Length);
                ApproximateVerboseFormatter (default_builder, span);
                return default_builder.ToString ();
            }
        }

        public static void ApproximateVerboseFormatter (StringBuilder builder, TimeSpan span)
        {
            if (span.Days > 0) {
                builder.AppendFormat (Catalog.GetPluralString ("{0} day", "{0} days", span.Days), span.Days);
            } if (span.Hours > 0) {
                builder.AppendFormat (Catalog.GetPluralString ("{0} hour", "{0} hours", span.Hours), span.Hours);
            } else if (span.Minutes > 0) {
                builder.AppendFormat (Catalog.GetPluralString ("{0} minute", "{0} minutes", span.Minutes), span.Minutes);
            } else {
                builder.AppendFormat (Catalog.GetPluralString ("{0} second", "{0} seconds", span.Seconds), span.Seconds);
            }
        }
    }
}

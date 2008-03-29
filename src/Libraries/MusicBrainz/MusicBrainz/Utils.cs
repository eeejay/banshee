/***************************************************************************
 *  Utils.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Text;

namespace MusicBrainz
{
    internal static class Utils
    {
        public static string EnumToString (Enum enumeration)
        {
            string str = enumeration.ToString ();
            StringBuilder builder = new StringBuilder (str.Length);
            builder.Append (str [0]);
            for (int i = 1; i < str.Length; i++) {
                if (str [i] >= 'A' && str [i] <= 'Z')
                    builder.Append ('-'); 
                builder.Append (str [i]);
            }
            return builder.ToString ();
        }
        
        public static T StringToEnum<T> (string name) where T : struct
        {
            return StringToEnumOrNull<T> (name) ?? default (T);
        }
        
        public static T? StringToEnumOrNull<T> (string name) where T : struct
        {
            if (name != null)
                foreach (T value in Enum.GetValues (typeof (T)))
                    if (Enum.GetName (typeof (T), value) == name)
                        return value;
            return null;
        }
        
        public static string PercentEncode (string value)
        {
            StringBuilder builder = new StringBuilder ();
            PercentEncode (builder, value);
            return builder.ToString ();
        }
        
        public static void PercentEncode (StringBuilder builder, string value)
        {
            foreach (char c in value) {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || 
                    c == '-' || c == '_' || c == '.' || c == '~')
                    builder.Append (c);
                else {
                    builder.Append ('%');
                    foreach (byte b in Encoding.UTF8.GetBytes (new char [] { c }))
                        builder.Append (string.Format ("{0:X}", b));
                } 
            }
        }
    }
}

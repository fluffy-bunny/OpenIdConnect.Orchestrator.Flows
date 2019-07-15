﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace OIDCPipeline.Core.Extensions
{
    internal static class NameValueCollectionExtensions
    {
   
        public static void Merge(this NameValueCollection collection, NameValueCollection extras)
        {
            if (extras == null) return;
            foreach (string key in extras)
            {
                // Overwrite any entry already there    
                collection[key] = extras[key];
            }
        }
        public static void Merge<T>(this NameValueCollection collection, Dictionary<string,T> extras) 
        {
            if (extras == null) return;
            foreach (var item in extras)
            {
                // Overwrite any entry already there    
                collection[item.Key] = item.Value.ToString() ;
            }
        }
        public static string ToQueryString(this NameValueCollection collection)
        {
            if (collection.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(128);
            var first = true;
            foreach (string name in collection)
            {
                var values = collection.GetValues(name);
                if (values == null || values.Length == 0)
                {
                    first = AppendNameValuePair(builder, first, true, name, string.Empty);
                }
                else
                {
                    foreach (var value in values)
                    {
                        first = AppendNameValuePair(builder, first, true, name, value);
                    }
                }
            }

            return builder.ToString();
        }

        public static string ToFormPost(this NameValueCollection collection)
        {
            var builder = new StringBuilder(128);
            const string inputFieldFormat = "<input type='hidden' name='{0}' value='{1}' />\n";

            foreach (string name in collection)
            {
                var values = collection.GetValues(name);
                var value = values.First();
                value = HtmlEncoder.Default.Encode(value);
                builder.AppendFormat(inputFieldFormat, name, value);
            }

            return builder.ToString();
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> data)
        {
            var result = new NameValueCollection();

            if (data == null || data.Count == 0)
            {
                return result;
            }

            foreach (var name in data.Keys)
            {
                var value = data[name];
                if (value != null)
                {
                    result.Add(name, value);
                }
            }

            return result;
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection collection)
        {
            return collection.ToScrubbedDictionary();
        }

        public static Dictionary<string, string> ToScrubbedDictionary(this NameValueCollection collection, params string[] nameFilter)
        {
            var dict = new Dictionary<string, string>();

            if (collection == null || collection.Count == 0)
            {
                return dict;
            }

            foreach (string name in collection)
            {
                var value = collection.Get(name);
                if (value != null)
                {
                    if (nameFilter.Contains(name))
                    {
                        value = "***REDACTED***";
                    }
                    dict.Add(name, value);
                }
            }

            return dict;
        }

        internal static string ConvertFormUrlEncodedSpacesToUrlEncodedSpaces(string str)
        {
            if (str != null && str.IndexOf('+') >= 0)
            {
                str = str.Replace("+", "%20");
            }
            return str;
        }

        private static bool AppendNameValuePair(StringBuilder builder, bool first, bool urlEncode, string name, string value)
        {
            var effectiveName = name ?? string.Empty;
            var encodedName = urlEncode ? UrlEncoder.Default.Encode(effectiveName) : effectiveName;

            var effectiveValue = value ?? string.Empty;
            var encodedValue = urlEncode ? UrlEncoder.Default.Encode(effectiveValue) : effectiveValue;
            encodedValue = ConvertFormUrlEncodedSpacesToUrlEncodedSpaces(encodedValue);

            if (first)
            {
                first = false;
            }
            else
            {
                builder.Append("&");
            }

            builder.Append(encodedName);
            if (!string.IsNullOrEmpty(encodedValue))
            {
                builder.Append("=");
                builder.Append(encodedValue);
            }
            return first;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace i18n
{
    /// <summary>
    /// Describes an item in a list of languages, such as in the HTTP Accept-Language header.
    /// This includes the language tag, quality, and position of the item in the list.
    /// </summary>
    /// <remarks>
    /// This is a value type to improve efficiency of building arrays of this type.
    /// Effort taken to reduce likelihood of boxing when addressing instances of this type.
    /// LanguageTag instances are typically re-used from a global cache, hence no allocations.
    /// </remarks>
    public struct LanguageItem : IComparable<LanguageItem>
    {
    // Data
        /// <summary>
        /// Describes a language.
        /// </summary>
        public ILanguageTag LanguageTag;

        /// <returns>
        /// A real number ranging from 0 to 1 describing the quality of the language tag relative
        /// to another for which an equivalent quality value is availble (0 = lowest quality; 1 = highest quality).
        /// As used in HTTP Accept-Language and Content-Language headers.
        /// </returns>
        public float Quality;

        /// <summary>
        /// Zero-based index of the item in the source language list.
        /// Used in comparison when Quality is equal.
        /// </summary>
        public int Ordinal;
    // Con
        public LanguageItem(
            ILanguageTag i_LanguageTag,
            float i_Quality,
            int i_Ordinal)
        {
            LanguageTag = i_LanguageTag;
            Quality = i_Quality;
            Ordinal = i_Ordinal;
        }
    // [IComparable<LanguageItem>]
        /// <summary>
        /// Facilitates ordering of language items to match their order in a source language list.
        /// Quality value is given precendence (higher sorts before lower value), but where that is equal, 
        /// we fallback on Ordinal (lower sorts before higher value).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(LanguageItem other)
        {
            return Quality != other.Quality ? (Quality > other.Quality ? -1 : 1):
                   Ordinal != other.Ordinal ? (Ordinal < other.Ordinal ? -1 : 1):
                   0;
        }
    // [Object]
        public override string ToString()
        {
            return string.Format("{0};q={1} ({2})", LanguageTag, Quality, Ordinal);
        }
    // Static helpers
        /// <summary>
        /// Parses an HTTP Accept-Language or Content-Language header value, returning
        /// a representative ordered array of LanguageItem, sorted in order or language
        /// preference.
        /// E.g. "de;q=0.5, en;q=1, fr-FR;q=0,ga;q=0.5".
        /// Notably, is able to re-order elements based on quality.
        /// </summary>
        /// <remarks>
        /// This method is designed to be as efficient as possible, typically requiring
        /// only a single heap alloc, for the returned array object itself.
        /// </remarks>
        /// <param name="headerval">
        /// HTTP Accept-Language header value.
        /// http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html.
        /// </param>
        public static LanguageItem[] ParseHttpLanguageHeader(string headerval)
        {
        // This method is designed to be as efficient as possible (avoiding string allocations where possible).
        //
            if (null == headerval) {
                throw new ArgumentNullException("headerval"); }
            int begin, end, pos1;
            int len = headerval.Length;
            int ordinal = 0;
           // Init array with enough elements for each language entry in the header.
            var LanguageItems = new LanguageItem[headerval.CountOfChar(',') + 1];
           // For each language component of the header (delimited by comma)
            for (begin = 0; begin < len; begin = end +1) {
                end = headerval.IndexOf(',', begin);
                if (-1 == end) {
                    end = len; }
                float qvalue = 1;
                pos1 = headerval.IndexOf(';', begin);
                if (-1 != pos1
                    && pos1 < end) {
                   // pos1 -> ";q=n"
                    if (pos1 -begin < 2 // room for valid langtag
                        || pos1 + 3 >= headerval.Length
                        || headerval[pos1 + 1] != 'q'
                        || headerval[pos1 + 2] != '=') {
                        continue; }
                    if (!ParseHelpers.TryParseDecimal(headerval, pos1 + 3, -1, out qvalue)) {
                        continue; }
                    if (qvalue < 0f || qvalue > 1.0f) {
                        continue; }
                }
                else {
                    pos1 = end; }
               // Skip over any whitespace. We expect this to make the following Trim redundant,
               // thus saving on an alloc.
                while (headerval[begin] == ' ') ++begin;
               // Extract language subtag e.g. "fr-FR".
               // NB: we expect this to be efficient and not allocate a new string as
               // a string matching the trimmed value is most likely already Intern (held by
               // the LanguageTag cache as a key value). Only first time here for a particular
               // value will a new string possibly be allocated.
                string langtag = headerval.Substring(begin, pos1 -begin).Trim();
               // Wrap langtag.
                LanguageTag lt = i18n.LanguageTag.GetCachedInstance(langtag);
                if (!lt.Language.IsSet()) {
                    continue; }
               // Store a new representative item.
               // NB: LanguageItem is a value type so no alloc done here.
                LanguageItems[ordinal] = new LanguageItem(lt, qvalue, ordinal);
                ++ordinal;
            }
           // Rearrange items into order of precedence. This is facilitated by LanguageItem's
           // impl. of IComparable.
            Array.Sort(LanguageItems);
           // Done.
            return LanguageItems;
        }
    }
}
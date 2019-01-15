using System;
using System.Linq;

namespace SuiBot_Core.Extensions.SuiStringExtension
{
    static class SuiStringExtension
    {
        /// <summary>
        /// Checks whatever the source string starts with provided text, while ognoring the character case.
        /// </summary>
        /// <param name="v">Source string</param>
        /// <param name="text">String to compare to.</param>
        /// <returns>Boolean value</returns>
        public static bool StartsWithLazy(this string v, string text)
        {
            return v.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Checks whatever the source strings starts with any of variables provided in an array, while ignoring the character case.
        /// </summary>
        /// <param name="v">Source text</param>
        /// <param name="texts">Array of variables to compare with.</param>
        /// <returns>Boolean value</returns>
        public static bool StartsWithLazy(this string v, string[] texts)
        {
            foreach(var text in texts)
            {
                if (v.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whatever the source string starts with provided text (and space after it), while ognoring the character case.
        /// </summary>
        /// <param name="v">Source string</param>
        /// <param name="text">String to compare to.</param>
        /// <returns>Boolean value</returns>
        public static bool StartsWithWordLazy(this string v, string text)
        {
            return v.StartsWith(text + " ", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Checks whatever the source strings starts with any of variables provided in an array (plus space), while ignoring the character case.
        /// </summary>
        /// <param name="v">Source text</param>
        /// <param name="texts">Array of variables to compare with.</param>
        /// <returns>Boolean value</returns>
        public static bool StartsWithWordLazy(this string v, string[] texts)
        {
            foreach (var text in texts)
            {
                if (v.StartsWith(text + " ", StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Strips a single word (till first space sign) from a string, additionally trimming whitespaces after strip.
        /// </summary>
        /// <param name="v">Source string</param>
        /// <returns>Source string stripped of a word.</returns>
        public static string StripSingleWord(this string v)
        {
            if (v.Contains(' '))
            {
                return v.Split(new char[] { ' ' }, 2)[1].Trim();
            }
            else
                return "";
        }
    }
}

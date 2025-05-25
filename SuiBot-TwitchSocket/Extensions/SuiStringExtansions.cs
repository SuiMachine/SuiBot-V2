using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuiBot_Core.Extensions.SuiStringExtension
{
	public static class SuiStringExtension
	{
		/// <summary>
		/// This is a string provider for formatting DateTime obtained from Twitch
		/// </summary>
		public const string Rfc3339FormatString = "yyyy-MM-dd'T'HH:mm:ss'Z'";


		/// <summary>
		/// Checks whatever the source string starts with provided text, while ignoring the character case.
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
			foreach (var text in texts)
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
				return v.Split(new char[] { ' ' }, 2)[1].Trim();
			else
				return "";
		}

		/// <summary>
		/// Picks a first word from a sentences and trims it
		/// </summary>
		/// <param name="v">Sentence from which to pick first word</param>
		/// <returns>First word or empty string</returns>
		public static string FirstWord(this string v)
		{
			if (v.Contains(' '))
			{
				return v.Split(new char[] { ' ' }, 2)[0].Trim();
			}
			else
				return "";
		}

		/// <summary>
		/// Splits string into chunks similiar as split, except gets chunks in between seperator
		/// </summary>
		/// <param name="v">String to perform it on.</param>
		/// <param name="chr">Seperator</param>
		/// <returns>Chars array.</returns>
		public static string[] GetChunks(this string v, char chr)
		{
			if (v.Contains(chr))
			{
				var split = v.Split(chr);
				if (split.Length < 2)
					return new string[0];
				else
				{
					List<string> tmpSplit = new List<string>();
					for (int i = 1; i < split.Length; i += 2)
					{
						tmpSplit.Add(split[i].Trim());
					}
					return tmpSplit.ToArray();
				}
			}
			else
				return new string[0];
		}

		public static string TrimSingleCharacter(this string v, char chr)
		{
			v = v.Trim();
			if (v.StartsWith(chr.ToString()))
			{
				v = v.Substring(1);
			}
			if (v.EndsWith(chr.ToString()))
			{
				v = v.Remove(v.Length - 1);
			}
			return v;
		}

		public static string GetUrlSafeString(this string v)
		{
			return Uri.EscapeDataString(v);
		}

		public static List<string> SplitMessage(this string v, int length)
		{
			if (v.Length <= length)
				return new List<string>() { v };

			var result = new List<string>();
			var split = v.Split(' ');

			var stringBuilder = new StringBuilder(500);
			for (int i = 0; i < split.Length; i++)
			{
				if (stringBuilder.Length + 1 + split[i].Length > 500)
				{
					result.Add(stringBuilder.ToString());
					stringBuilder.Clear();
				}

				if(stringBuilder.Length > 0)
					stringBuilder.Append(' ');

				stringBuilder.Append(split[i]);
			}

			result.Add(stringBuilder.ToString());
			return result;
		}
	}
}

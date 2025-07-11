﻿using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core.Components
{
	internal class Quotes
	{
		//Magick
		const string RegexFindID = "(id(:|=)\\\")(\\w+)\\\"|id(:|=)\\w+";
		const string RegexFindAuthor = "author(:|=)\".+?\"|author(:|=)\\w+";
		const string RegexFindQuote = "quote[:=]([\\\"\\\'])(?:(?=(\\\\?))\\2.)*?\\1|quote[:=]\\w+";

		SuiBot_ChannelInstance ChannelInstance { get; set; }
		Storage.Quotes ChannelQuotes { get; set; }

		readonly Random rng = new Random();

		public Quotes(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
			ChannelQuotes = Storage.Quotes.Load(ChannelInstance.Channel);
		}

		internal void DoWork(ES_ChatMessage lastMessage)
		{
			string msg = lastMessage.message.text.StripSingleWord();

			if (msg.StartsWithWordLazy("add"))
			{
				AddQuote(lastMessage, msg);
			}
			else if (msg.StartsWithWordLazy(new string[] { "remove", "delete" }))
			{
				RemoveQuote(lastMessage, msg);
			}
			else if (msg.StartsWithWordLazy(new string[] { "search", "find" }))
			{
				SearchQuote(lastMessage, msg);
			}
			else if (msg.StartsWithLazy("last"))
			{
				GetQuote(lastMessage, msg, ChannelQuotes.QuotesList.Count - 1);
			}
			else
			{
				GetQuote(lastMessage, msg);
			}
		}

		private void SearchQuote(ES_ChatMessage lastMessage, string strippedText)
		{
			FilterOutSegments(strippedText, out int quoteID, out string authorFilter, out string quoteTextFilter);
			if (quoteID == -2)
				quoteID = ChannelQuotes.QuotesList.Count - 1;

			if (quoteID < 0 && authorFilter == "" && quoteTextFilter == "")
				ChannelInstance.SendChatMessageResponse(lastMessage, "No search data provided or incorrect format");
			else if (quoteID >= 0 && (authorFilter != "" || quoteTextFilter != ""))
			{
				ChannelInstance.SendChatMessageResponse(lastMessage, "ID search is not meant to be used in conjunction with Author Filter or Quote Text Filter!");
			}
			else if (quoteID >= 0)
			{
				if (quoteID < ChannelQuotes.QuotesList.Count)
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, ChannelQuotes.QuotesList[quoteID].ToString());
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Provided ID is higher than total count of Quotes (total: {0}).", ChannelQuotes.QuotesList.Count));
			}
			else
			{

				Regex authorRegex = null;
				Regex quoteRegex = null;
				try
				{
					if (authorFilter != "")
						authorRegex = new Regex(authorFilter, RegexOptions.IgnoreCase);
				}
				catch
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, "Author regex failed to compile");
					return;
				}

				try
				{
					if (quoteTextFilter != "")
						quoteRegex = new Regex(quoteTextFilter, RegexOptions.IgnoreCase);
				}
				catch
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, "Quote regex failed to compile");
					return;
				}


				var advSearchQuotes = AdvancedSearch(authorRegex, quoteRegex);
				if (advSearchQuotes == null)
					ChannelInstance.SendChatMessageResponse(lastMessage, "Nothing found.");
				else
				{
					if (advSearchQuotes.Length > 1)
					{
						List<int> ids = new List<int>();
						foreach (var foundQuote in advSearchQuotes)
						{
							ids.Add(ChannelQuotes.QuotesList.IndexOf(foundQuote));
						}
						ChannelInstance.SendChatMessageResponse(lastMessage, $"Found {advSearchQuotes.Length} results: {string.Join(", ", ids).TrimEnd(',')}.");
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMessage, advSearchQuotes[0].ToString());
				}
			}
		}

		/// <summary>
		/// Searches out QuoteList using author's name, quote or combination of both (originally intended for regex search)
		/// </summary>
		/// <param name="author">Author's name.</param>
		/// <param name="quote">Quote.</param>
		/// <returns>Array of matched quotes or null if none found.</returns>
		private Storage.Quote[] AdvancedSearch(string author, string quote)
		{
			if (author == "")
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => x.Text == quote);
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
			else if (quote == "")
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => x.Author == author);
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
			else
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => (x.Author == author && x.Text == quote));
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
		}

		private Storage.Quote[] AdvancedSearch(Regex authorSearchPattern, Regex quoteSearchPattern)
		{
			if (authorSearchPattern == null)
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => quoteSearchPattern.IsMatch(x.Text));
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
			else if (quoteSearchPattern == null)
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => authorSearchPattern.IsMatch(x.Author));
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
			else
			{
				var filteredOut = ChannelQuotes.QuotesList.Where(x => authorSearchPattern.IsMatch(x.Author) && quoteSearchPattern.IsMatch(x.Text));
				if (filteredOut.Count() > 0)
					return filteredOut.ToArray();
				else
					return null;
			}
		}

		/// <summary>
		/// Separates quoteID, Author's Name and Quote from a provided string (quote syntax)
		/// </summary>
		/// <param name="message">Message provided by a user.</param>
		/// <param name="quoteID">Id of a quote, if provided in message. -1 if none found or error.</param>
		/// <param name="authorFilter">Author's name. String.Empty if none found.</param>
		/// <param name="quoteTextFilter">Quote from a provided message. String.Empty if none found.</param>
		private static void FilterOutSegments(string message, out int quoteID, out string authorFilter, out string quoteTextFilter)
		{
			var idMatches = Regex.Matches(message, RegexFindID, RegexOptions.IgnoreCase);
			if (idMatches.Count > 0)
			{
				var potentialIdFilter = idMatches[0].Value.Remove(0, "id:".Length);
				potentialIdFilter = potentialIdFilter.Trim(new char[] { ' ', '\"' });

				if (potentialIdFilter.ToLower() == "last")
					quoteID = -2;       //Isn't it obvious it's added after the fact?
				else if (!int.TryParse(potentialIdFilter, out quoteID))
					quoteID = -1;
			}
			else
				quoteID = -1;

			var authorMatches = Regex.Matches(message, RegexFindAuthor, RegexOptions.IgnoreCase);
			if (authorMatches.Count > 0)
			{
				authorFilter = authorMatches[0].Value.Remove(0, "author:".Length);
				authorFilter = authorFilter.TrimSingleCharacter('\"');
			}
			else
				authorFilter = "";

			var quoteMatches = Regex.Matches(message, RegexFindQuote, RegexOptions.IgnoreCase);
			if (quoteMatches.Count > 0)
			{
				quoteTextFilter = quoteMatches[0].Value.Remove(0, "quote:".Length);
				quoteTextFilter = quoteTextFilter.TrimSingleCharacter('\"');
			}
			else
				quoteTextFilter = "";
		}

		private void GetQuote(ES_ChatMessage lastMessage, string strippedMessage, int id = -1)
		{
			if (ChannelQuotes.QuotesList.Count == 0)
				ChannelInstance.SendChatMessageResponse(lastMessage, "Channel doesn't have any quotes");
			else
			{
				if (id == -1)
				{
					var idOrFilter = strippedMessage;
					if (idOrFilter == "")
						ChannelInstance.SendChatMessageResponse(lastMessage, GetRandomQuote());
					else if (int.TryParse(idOrFilter, out var result))
					{
						if (result > ChannelQuotes.QuotesList.Count)
							ChannelInstance.SendChatMessageResponse(lastMessage, $"There is no quote with this ID (max ID + {ChannelQuotes.QuotesList.Count - 1}).");
						else
						{
							ChannelInstance.SendChatMessageResponse(lastMessage, ChannelQuotes.QuotesList[result].ToString());
						}
					}
					else
					{
						try
						{
							Regex searchRegex = new Regex(strippedMessage, RegexOptions.IgnoreCase);
							var possibleQuotes = ChannelQuotes.QuotesList.Where(x => searchRegex.IsMatch(x.Text)).ToList();
							if (possibleQuotes.Count > 0)
							{
								int randomQuoteId = rng.Next(possibleQuotes.Count);
								ChannelInstance.SendChatMessageResponse(lastMessage, possibleQuotes[randomQuoteId].ToString());
							}
							else
							{
								ChannelInstance.SendChatMessageResponse(lastMessage, "Nothing found. Use empty \"!quote\" to return random quote.");
							}
						}
						catch
						{
							ChannelInstance.SendChatMessageResponse(lastMessage, "Regex failed to compile");
						}
					}
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, ChannelQuotes.QuotesList[id].ToString());
			}
		}

		private string GetRandomQuote()
		{
			int quoteId = rng.Next(ChannelQuotes.QuotesList.Count);
			return ChannelQuotes.QuotesList[quoteId].ToString();
		}

		public void AddQuote(ES_ChatMessage lastMessage, string strippedMessage)
		{
			if (lastMessage.UserRole <= Role.VIP)
			{
				FilterOutSegments(strippedMessage, out int _, out string authorFilter, out string quoteTextFilter);
				if (quoteTextFilter != "")
				{
					var newQuote = new Storage.Quote(authorFilter, quoteTextFilter);
					ChannelQuotes.QuotesList.Add(newQuote);
					ChannelQuotes.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Added new quote (ID: {0}): {1}", ChannelQuotes.QuotesList.Count - 1, newQuote));
				}
			}
			else
			{
				ChannelInstance.SendChatMessage("Only VIPs and Moderators can add quotes. Sorry.");
			}
		}

		public void RemoveQuote(ES_ChatMessage lastMessage, string strippedMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				if (ChannelQuotes.QuotesList.Count == 0)
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, "Channel doesn't have any quotes.");
					return;
				}

				FilterOutSegments(strippedMessage, out int quoteID, out string authorFilter, out string quoteTextFilter);
				if (quoteID == -2)
					quoteID = ChannelQuotes.QuotesList.Count - 1;

				if (quoteID >= 0 && (authorFilter != "" || quoteTextFilter != ""))
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, "ID search is not meant to be used in conjunction with Author Filter or Quote Text Filter!");
				}
				else
				{
					if (quoteID < ChannelQuotes.QuotesList.Count && quoteID >= 0)
					{
						var quote = ChannelQuotes.QuotesList[quoteID];
						ChannelQuotes.QuotesList.RemoveAt(quoteID);
						ChannelQuotes.Save();
						ChannelInstance.SendChatMessageResponse(lastMessage, "Removed quote: " + quote.ToString());
					}
					else if (quoteID == -1)
					{
						var advSearch = AdvancedSearch(authorFilter, quoteTextFilter);
						if (advSearch == null)
						{
							ChannelInstance.SendChatMessageResponse(lastMessage, "Nothing found!");
						}
						else
						{
							if (advSearch.Length > 1)
								ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Found {0} results, so nothing was removed.", advSearch.Length));
							else
							{
								var removedQuote = advSearch[0];
								ChannelQuotes.QuotesList.Remove(removedQuote);
								ChannelQuotes.Save();
								ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Removed: {0}", removedQuote.ToString()));
							}
						}
					}
					else
					{
						ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Provided ID is higher than total count of Quotes (total: {0}).", ChannelQuotes.QuotesList.Count));
					}
				}
			}
			else
			{
				ChannelInstance.SendChatMessage("Only VIPs and Moderators can remove quotes. Sorry.");
			}
		}

		internal string AddQuote(string author, string quote)
		{
			ChannelQuotes.QuotesList.Add(new Storage.Quote(author, quote));
			ChannelQuotes.Save();
			if (string.IsNullOrEmpty(author))
				return $"Added quote \"{quote}\" with index {ChannelQuotes.QuotesList.Count - 1}";
			else
				return $"Added quote \"{quote}\" - {author} with index {ChannelQuotes.QuotesList.Count - 1}";
		}

		internal string FindQuote(string author, string quote)
		{
			var quotesList = ChannelQuotes.QuotesList;
			if (!string.IsNullOrEmpty(author))
			{
				Regex reg = new Regex(author, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				quotesList = quotesList.Where(x => reg.IsMatch(x.Author)).ToList();
			}

			if (!string.IsNullOrEmpty(quote))
			{
				Regex reg = new Regex(quote, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				quotesList = quotesList.Where(x => reg.IsMatch(x.Text)).ToList();
			}

			if (quotesList.Count == 0)
				return $"Couldn't find a specified quote";
			else if (quotesList.Count == 1)
			{
				var foundQuote = quotesList[0];
				var index = ChannelQuotes.QuotesList.IndexOf(foundQuote);
				if (string.IsNullOrEmpty(foundQuote.Author))
					return $"Found quote at index {index}. The quote is \"{foundQuote.Text}\".";
				else
					return $"Found quote at index {index}. The quote is \"{foundQuote.Text}\" by {foundQuote.Author}.";
			}
			else
			{
				var foundIndexes = string.Join(", ", quotesList.Select(x => ChannelQuotes.QuotesList.IndexOf(x)));
				return $"Found more than 1 quote matching. Found quotes have indexes: {foundIndexes}";
			}
		}

		internal string RemoveQuote(string author, string quote)
		{
			var quotesList = ChannelQuotes.QuotesList;
			if (!string.IsNullOrEmpty(author))
			{
				Regex reg = new Regex(author, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				quotesList = quotesList.Where(x => reg.IsMatch(x.Author)).ToList();
			}

			if (!string.IsNullOrEmpty(quote))
			{
				Regex reg = new Regex(quote, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				quotesList = quotesList.Where(x => reg.IsMatch(x.Text)).ToList();
			}

			if (quotesList.Count == 0)
				return $"Couldn't find a specified quote";
			else if (quotesList.Count == 1)
			{
				var foundQuote = quotesList[0];
				var index = ChannelQuotes.QuotesList.IndexOf(foundQuote);
				ChannelQuotes.QuotesList.Remove(foundQuote);
				ChannelQuotes.Save();
				if (string.IsNullOrEmpty(foundQuote.Author))
					return $"Deleted quote at index {index}. The quote was {foundQuote.Text}.";
				else
					return $"Deleted quote at index {index}. The quote was {foundQuote.Text} by {foundQuote.Author}.";
			}
			else
			{
				var foundIndexes = string.Join(", ", quotesList.Select(x => ChannelQuotes.QuotesList.IndexOf(x)));
				return $"Found more than 1 quote matching. Found quotes have indexes: {foundIndexes}";
			}
		}

		internal string FindQuoteByIndex(int index)
		{
			if (ChannelQuotes.QuotesList.Count == 0)
				return "Can't find a quote, because there are no quotes at all!";

			if(index >= 0 && index <  ChannelQuotes.QuotesList.Count)
			{
				var quote = ChannelQuotes.QuotesList[index];
				if (string.IsNullOrEmpty(quote.Author))
					return $"Found quote at index {index}. The quote is \"{quote.Text}\".";
				else
					return $"Found quote at index {index}. The quote is \"{quote.Text}\" by {quote.Author}.";
			}
			else
				return $"Can't find a quote, because it's outside the available range {0} - {ChannelQuotes.QuotesList.Count-1}";
		}

		internal string RemoveQuoteByIndex(int index)
		{
			if (ChannelQuotes.QuotesList.Count == 0)
				return "Can't delete a quote, because there are no quotes at all!";

			if (index >= 0 && index < ChannelQuotes.QuotesList.Count)
			{
				var quote = ChannelQuotes.QuotesList[index];
				ChannelQuotes.QuotesList.Remove(quote);
				ChannelQuotes.Save();
				if (string.IsNullOrEmpty(quote.Author))
				{
					return $"Deleted quote: \"{quote.Text}\".";
				}
				else
					return $"Deleted quote \"{quote.Text}\" by {quote.Author}.";
			}
			else
				return $"Can't delete a quote, because it's outside the available range {0} - {ChannelQuotes.QuotesList.Count - 1}";
		}
	}
}

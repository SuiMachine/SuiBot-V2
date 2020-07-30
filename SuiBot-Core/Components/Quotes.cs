using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace SuiBot_Core.Components
{
    internal class Quotes : IDisposable
    {
        //Magick
        const string RegexFindID = "(id(:|=)\\\")(\\w+)\\\"|id(:|=)\\w+";
        const string RegexFindAuthor = "author(:|=)\".+?\"|author(:|=)\\w+";
        const string RegexFindQuote = "quote(:|=)\".+?\"|quote(:|=)\\w+";

        SuiBot_ChannelInstance ChannelInstance { get; set; }
        Storage.Quotes ChannelQuotes { get; set; }
        Random rng = new Random();

        public Quotes(SuiBot_ChannelInstance ChannelInstance)
        {
            this.ChannelInstance = ChannelInstance;
            ChannelQuotes = Storage.Quotes.Load(ChannelInstance.Channel);
        }

        internal void DoWork(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();

            if (lastMessage.Message.StartsWithWordLazy("add"))
            {
                AddQuote(lastMessage);
            }
            else if (lastMessage.Message.StartsWithWordLazy(new string[] { "remove", "delete" }))
            {
                RemoveQuote(lastMessage);
            }
            else if (lastMessage.Message.StartsWithWordLazy(new string[] { "search", "find" }))
            {
                SearchQuote(lastMessage);
            }
            else if(lastMessage.Message.StartsWithLazy("last"))
            {
                GetQuote(lastMessage, ChannelQuotes.QuotesList.Count-1);
            }
            else
            {
                GetQuote(lastMessage);
            }
        }

        private void SearchQuote(ChatMessage lastMessage)
        {
            FilterOutSegments(lastMessage.Message, out int quoteID, out string authorFilter, out string quoteTextFilter);
            if (quoteID == -2)
                quoteID = ChannelQuotes.QuotesList.Count-1;

            if (quoteID < 0 && authorFilter == "" && quoteTextFilter == "")
                ChannelInstance.SendChatMessageResponse(lastMessage, "No search data provided?");
            else if (quoteID >= 0 && (authorFilter != "" || quoteTextFilter != ""))
            {
                ChannelInstance.SendChatMessageResponse(lastMessage, "ID search is not meant to be used in conjunction with Author Filter or Quote Text Filter!");
            }
            else if(quoteID >= 0)
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
                var advSearchQuotes = AdvancedSearch(authorFilter, quoteTextFilter);
                if(advSearchQuotes == null)
                    ChannelInstance.SendChatMessageResponse(lastMessage,"Nothing found.");
                else
                {
                    if(advSearchQuotes.Length > 1)
                        ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Found {0} results.", advSearchQuotes.Length));
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
            if(author == "")
            {
                var filteredOut = ChannelQuotes.QuotesList.Where(x => x.Text == quote);
                if (filteredOut.Count() > 0)
                    return filteredOut.ToArray();
                else
                    return null;
            }
            else if(quote == "")
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

        /// <summary>
        /// Seperates quoteID, Author's Name and Quote from a provided string (quote syntax)
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
                authorFilter = authorFilter.Trim(new char[] { ' ', '\"' });
            }
            else
                authorFilter = "";

            var quoteMatches = Regex.Matches(message, RegexFindQuote, RegexOptions.IgnoreCase);
            if (quoteMatches.Count > 0)
            {
                quoteTextFilter = quoteMatches[0].Value.Remove(0, "quote:".Length);
                quoteTextFilter = quoteTextFilter.Trim(new char[] { ' ', '\"' });
            }
            else
                quoteTextFilter = "";
        }

        private void GetQuote(ChatMessage lastMessage, int id = -1)
        {
            if(ChannelQuotes.QuotesList.Count == 0)
                ChannelInstance.SendChatMessageResponse(lastMessage, "Channel doesn't have any quotes");
            else if (id == -1)
                ChannelInstance.SendChatMessageResponse(lastMessage, GetRandomQuote(), true);
            else
                ChannelInstance.SendChatMessageResponse(lastMessage, ChannelQuotes.QuotesList[id].ToString(), true);
        }

        private string GetRandomQuote()
        {
            int quoteId = rng.Next(ChannelQuotes.QuotesList.Count);
            return ChannelQuotes.QuotesList[quoteId].ToString();
        }

        public void AddQuote(ChatMessage lastMessage)
        {
            if(lastMessage.UserRole <= Role.VIP)
            {
                FilterOutSegments(lastMessage.Message, out int quoteID, out string authorFilter, out string quoteTextFilter);
                if(quoteTextFilter != "")
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

        public void RemoveQuote(ChatMessage lastMessage)
        {
            if (lastMessage.UserRole <= Role.Mod)
            {
                if(ChannelQuotes.QuotesList.Count == 0)
                {
                    ChannelInstance.SendChatMessageResponse(lastMessage, "Channel doesn't have any quotes.");
                    return;
                }

                FilterOutSegments(lastMessage.Message, out int quoteID, out string authorFilter, out string quoteTextFilter);
                if (quoteID == -2)
                    quoteID = ChannelQuotes.QuotesList.Count - 1;

                if (quoteID >= 0 && (authorFilter != "" || quoteTextFilter != ""))
                {
                    ChannelInstance.SendChatMessageResponse(lastMessage, "ID search is not meant to be used in conjunction with Author Filter or Quote Text Filter!");
                }
                else
                {
                    if(quoteID < ChannelQuotes.QuotesList.Count && quoteID >= 0)
                    {
                        var quote = ChannelQuotes.QuotesList[quoteID];
                        ChannelQuotes.QuotesList.RemoveAt(quoteID);
                        ChannelQuotes.Save();
                        ChannelInstance.SendChatMessageResponse(lastMessage, "Removed quote: " + quote.ToString());
                    }
                    else if(quoteID == -1)
                    {
                        var advSearch = AdvancedSearch(authorFilter, quoteTextFilter);
                        if(advSearch == null)
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

        public void Dispose()
        {
        }
    }
}

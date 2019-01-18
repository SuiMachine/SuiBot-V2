using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using SuiBot_Core.Extensions.SuiStringExtension;

namespace SuiBot_Core.Components
{
    internal class ChatFiltering : IDisposable
    {
        const string RegexFindTimeoutLenght = "duration:\".+?\"";
        const string RegexFindResponse = "response:\".+?\"";

        SuiBot_ChannelInstance ChannelInstance;
        Storage.ChatFilters Filters;
        Storage.ChatFilterUsersDB UserDB;

        public ChatFiltering(SuiBot_ChannelInstance ChannelInstance)
        {
            this.ChannelInstance = ChannelInstance;
            Filters = Storage.ChatFilters.Load(ChannelInstance.Channel);
            UserDB = Storage.ChatFilterUsersDB.Load(ChannelInstance.Channel);
        }

        public void Dispose()
        {
            UserDB.Save();
            Filters = null;
            UserDB = null;
            ChannelInstance = null;
        }

        enum FilterType
        {
            Purge,
            Timeout,
            Ban
        }

        public void DoWork(ChatMessage lastMassage)
        {
            if(lastMassage.UserRole <= Role.Mod)
            {
                lastMassage.Message = lastMassage.Message.StripSingleWord();
                if (lastMassage.Message.StartsWithWordLazy("add"))
                {
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (lastMassage.Message.StartsWithWordLazy("purge"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("timeout"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("ban"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter add messages should be followed by purge / timeout / ban");
                }
                else if (lastMassage.Message.StartsWithWordLazy(new string[] { "remove", "delete" }))
                {
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (lastMassage.Message.StartsWithWordLazy("purge"))
                    {
                        RemoveFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("timeout"))
                    {
                        RemoveFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("ban"))
                    {
                        RemoveFilter(FilterType.Purge, lastMassage);
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter remove messages should be followed by purge / timeout / ban and then filter ID.");
                }
                else if (lastMassage.Message.StartsWithWordLazy(new string[] { "search", "find" }))
                {
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (lastMassage.Message.StartsWithWordLazy("purge"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("timeout"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("ban"))
                    {
                        AddFilter(FilterType.Purge, lastMassage);
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter add messages should be followed by purge / timeout / ban");
                }
                else if (lastMassage.Message.StartsWithWordLazy("update"))
                {
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (lastMassage.Message.StartsWithWordLazy("purge"))
                    {
                        UpdateFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("timeout"))
                    {
                        UpdateFilter(FilterType.Purge, lastMassage);
                    }
                    else if (lastMassage.Message.StartsWithWordLazy("ban"))
                    {
                        UpdateFilter(FilterType.Purge, lastMassage);
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter update messages should be followed by purge / timeout / ban, then ID or Last and then parsabl information.");
                }
                else
                    ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. ChatFilter messages should be followed by add / remove / find / update.");
            }

        }

        /// <summary>
        /// Adds a new filter.
        /// </summary>
        /// <param name="filterType">Type of filter to be added based on FilterType enum.</param>
        /// <param name="lastMassage">Message to get the filter from.</param>
        private void AddFilter(FilterType filterType, ChatMessage lastMassage)
        {
            lastMassage.Message = lastMassage.Message.StripSingleWord();
            if (lastMassage.Message == "")
            {
                ChannelInstance.SendChatMessageResponse(lastMassage, "Filter can not be empty!");
                return;
            }

            switch (filterType)
            {
                case (FilterType.Purge):
                    {
                        var newFilter = lastMassage.Message;
                        Filters.PurgeFilters.Add(new Storage.ChatFilter(newFilter, "", 1));
                        Filters.Save();
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update purge last Response:\"Custom response\"");
                    }
                    break;
                case (FilterType.Timeout):
                    {
                        var newFilter = lastMassage.Message;
                        Filters.TimeOutFilter.Add(new Storage.ChatFilter(newFilter, "", 1));
                        Filters.Save();
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update timeout last Response:\"Custom response\", Duration:\"Lenght\"");
                    }
                    break;
                case (FilterType.Ban):
                    {
                        var newFilter = lastMassage.Message;
                        Filters.BanFilters.Add(new Storage.ChatFilter(newFilter, "", 1));
                        Filters.Save();
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update ban last Response:\"Custom response\"");
                    }
                    break;
            }
        }

        /// <summary>
        /// Updates a filter with Response and Duration for timeout.
        /// </summary>
        /// <param name="filterType">Type of filter to be added based on FilterType enum.</param>
        /// <param name="lastMassage">Message from which to get Response and Duration from.</param>
        private void UpdateFilter(FilterType filterType, ChatMessage lastMassage)
        {
            lastMassage.Message = lastMassage.Message.StripSingleWord();
            if (lastMassage.Message == "")
            {
                ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be empty!");
                return;
            }
            else if (lastMassage.Message.StartsWithLazy("last"))
            {
                lastMassage.Message = lastMassage.Message.StripSingleWord();

                switch (filterType)
                {
                    case (FilterType.Purge):
                        {
                            if (Filters.PurgeFilters.Count > 0)
                            {
                                AdvancedFilters(lastMassage.Message, out string Response, out uint Lenght);
                                var filter = Filters.PurgeFilters.Last();
                                filter.Duration = 1;
                                filter.Response = Response;
                                Filters.PurgeFilters[Filters.PurgeFilters.Count - 1] = filter;
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Updated purge filter with response: " + Response);
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters definied.");
                        }
                        break;
                    case (FilterType.Timeout):
                        {
                            if (Filters.TimeOutFilter.Count > 0)
                            {
                                AdvancedFilters(lastMassage.Message, out string Response, out uint Lenght);
                                var filter = Filters.TimeOutFilter.Last();
                                filter.Duration = Lenght;
                                filter.Response = Response;
                                Filters.TimeOutFilter[Filters.TimeOutFilter.Count - 1] = filter;
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Updated timeout filter with duration of " + Lenght.ToString() + " seconds and response: " + Response);
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters definied.");
                        }
                        break;
                    case (FilterType.Ban):
                        {
                            if (Filters.BanFilters.Count > 0)
                            {
                                AdvancedFilters(lastMassage.Message, out string Response, out uint Lenght);
                                var filter = Filters.BanFilters.Last();
                                filter.Duration = 1;
                                filter.Response = Response;
                                Filters.BanFilters[Filters.BanFilters.Count - 1] = filter;
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Updated ban filter with response: " + Response);
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no ban filters definied.");
                        }
                        break;
                }
            }
            else
            {
                if (lastMassage.Message.Contains(" "))
                {
                    string filterIDAsString = lastMassage.Message.Split(new char[] { ' ' })[0];
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (int.TryParse(filterIDAsString, out int id))
                    {
                        if (id < 0)
                        {
                            ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be negative!");
                        }
                        else
                        {
                            switch (filterType)
                            {
                                case (FilterType.Purge):
                                    {
                                        if(id >= Filters.PurgeFilters.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.PurgeFilters.Count -1));
                                        else
                                        {
                                            AdvancedFilters(lastMassage.Message, out string responseText, out uint lenght);
                                            var filter = Filters.PurgeFilters[id];
                                            filter.Duration = 1;
                                            filter.Response = responseText;
                                            Filters.PurgeFilters[id] = filter;
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Updated purge filter {0} with a response: {1}", id, responseText));
                                        }
                                    }
                                    break;
                                case (FilterType.Timeout):
                                    {
                                        if (id >= Filters.TimeOutFilter.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.TimeOutFilter.Count - 1));
                                        else
                                        {
                                            AdvancedFilters(lastMassage.Message, out string responseText, out uint lenght);
                                            var filter = Filters.TimeOutFilter[id];
                                            filter.Duration = lenght;
                                            filter.Response = responseText;
                                            Filters.TimeOutFilter[id] = filter;
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Updated timeout filter {0} with duration of {1} and response: {2}", id, lenght, responseText));
                                        }
                                    }
                                    break;
                                case (FilterType.Ban):
                                    {
                                        if (id >= Filters.BanFilters.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.BanFilters.Count - 1));
                                        else
                                        {
                                            AdvancedFilters(lastMassage.Message, out string responseText, out uint lenght);
                                            var filter = Filters.BanFilters[id];
                                            filter.Duration = 1;
                                            filter.Response = responseText;
                                            Filters.BanFilters[id] = filter;
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Updated ban filter {0} with a response: {1}", id, responseText));
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Failed to parse ID.");
                }
                else
                    ChannelInstance.SendChatMessageResponse(lastMassage, "No ID provided");
            }
        }

        /// <summary>
        /// Removes the filter using a given ID.
        /// </summary>
        /// <param name="filterType">Type of filter to be removed based on FilterType enum.</param>
        /// <param name="lastMassage">Message from which to get id from.</param>
        private void RemoveFilter(FilterType filterType, ChatMessage lastMassage)
        {
            lastMassage.Message = lastMassage.Message.StripSingleWord();
            if (lastMassage.Message == "")
            {
                ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be empty!");
                return;
            }
            else if (lastMassage.Message.StartsWithLazy("last"))
            {
                lastMassage.Message = lastMassage.Message.StripSingleWord();

                switch (filterType)
                {
                    case (FilterType.Purge):
                        {
                            if (Filters.PurgeFilters.Count > 0)
                            {
                                Filters.PurgeFilters.RemoveAt(Filters.PurgeFilters.Count - 1);
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Removed last purge filter.");
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters definied.");
                        }
                        break;
                    case (FilterType.Timeout):
                        {
                            if (Filters.TimeOutFilter.Count > 0)
                            {
                                Filters.TimeOutFilter.RemoveAt(Filters.TimeOutFilter.Count - 1);
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Removed last timeout filter.");
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters definied.");
                        }
                        break;
                    case (FilterType.Ban):
                        {
                            if (Filters.BanFilters.Count > 0)
                            {
                                Filters.BanFilters.RemoveAt(Filters.BanFilters.Count - 1);
                                Filters.Save();
                                ChannelInstance.SendChatMessageResponse(lastMassage, "Removed last ban filter.");
                            }
                            else
                                ChannelInstance.SendChatMessageResponse(lastMassage, "There are no ban filters definied.");
                        }
                        break;
                }
            }
            else
            {
                if (lastMassage.Message.Contains(" "))
                {
                    string filterIDAsString = lastMassage.Message.Split(new char[] { ' ' })[0];
                    lastMassage.Message = lastMassage.Message.StripSingleWord();
                    if (int.TryParse(filterIDAsString, out int id))
                    {
                        if (id < 0)
                        {
                            ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be negative!");
                        }
                        else
                        {
                            switch (filterType)
                            {
                                case (FilterType.Purge):
                                    {
                                        if (id >= Filters.PurgeFilters.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.PurgeFilters.Count - 1));
                                        else
                                        {
                                            Filters.PurgeFilters.RemoveAt(id);
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Removed purge filter {0}", id));
                                        }
                                    }
                                    break;
                                case (FilterType.Timeout):
                                    {
                                        if (id >= Filters.TimeOutFilter.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.TimeOutFilter.Count - 1));
                                        else
                                        {
                                            Filters.PurgeFilters.RemoveAt(id);
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Removed timeout filter {0}", id));
                                        }
                                    }
                                    break;
                                case (FilterType.Ban):
                                    {
                                        if (id >= Filters.BanFilters.Count)
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.BanFilters.Count - 1));
                                        else
                                        {
                                            Filters.PurgeFilters.RemoveAt(id);
                                            Filters.Save();
                                            ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Removed ban filter {0}", id));
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMassage, "Failed to parse ID.");
                }
                else
                    ChannelInstance.SendChatMessageResponse(lastMassage, "No ID provided");
            }

        }

        /// <summary>
        /// Function that seperates response text and duration lenght from a message (Filter syntax)
        /// </summary>
        /// <param name="message">Message from which to seperate response and duration from.</param>
        /// <param name="response">Response text (String.Empty if none provided).</param>
        /// <param name="lenght">Duration lenght (1 if none provided of error)</param>
        private void AdvancedFilters(string message, out string response, out uint lenght)
        {
            var responseMatches = Regex.Matches(message, RegexFindResponse, RegexOptions.IgnoreCase);
            if (responseMatches.Count > 0)
            {
                response = responseMatches[0].Value.Remove(0, "response:".Length).Trim(new char[] { ' ', '\"' });
            }
            else
                response = "";

            var lenghtMatches = Regex.Matches(message, RegexFindTimeoutLenght, RegexOptions.IgnoreCase);
            if (lenghtMatches.Count > 0)
            {
                var timeStripped = responseMatches[0].Value.Remove(0, "duration:".Length).Trim(new char[] { ' ', '\"' });
                if (!uint.TryParse(timeStripped, out lenght))
                    lenght = 1;
            }
            else
                lenght = 1;
        }

        /// <summary>
        /// Iterates through all filters and takes action if needed
        /// </summary>
        /// <param name="lastMassage">Message to iterate through</param>
        /// <returns>Boolean value indicating whatever an action needed to be taken or not.</returns>
        public bool FilterOutMessages(ChatMessage lastMassage)
        {
            int id = 0;
            foreach(var filter in Filters.PurgeFilters)
            {
                if (Regex.IsMatch(lastMassage.Message, filter.Syntax, RegexOptions.IgnoreCase))
                {
                    ChannelInstance.UserPurge(lastMassage.Username, string.Format("{0} (filterID: {1})", filter.Response, id));
                    return true;
                }
                id++;
            }

            id = 0;
            foreach (var filter in Filters.TimeOutFilter)
            {
                if (Regex.IsMatch(lastMassage.Message, filter.Syntax, RegexOptions.IgnoreCase))
                {
                    ChannelInstance.UserTimeout(lastMassage.Username, filter.Duration, string.Format("{0} (filterID: {1})", filter.Response, id));
                    return true;
                }
            }

            id = 0;
            foreach (var filter in Filters.BanFilters)
            {
                if (Regex.IsMatch(lastMassage.Message, filter.Syntax, RegexOptions.IgnoreCase))
                {
                    ChannelInstance.UserBan(lastMassage.Username, string.Format("{0} (filterID: {1})", filter.Response, id));
                    return true;
                }
                id++;
            }
            return false;
        }
    }
}

using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SuiBot_Core.Components
{
	internal class ChatFiltering : IDisposable
	{
		const string RegexFindTimeoutLenght = "duration(:|=)\".+?\"";
		const string RegexFindResponse = "response(:|=)\".+?\"";
		static readonly List<string> UrlMarks = new List<string>() { "http://", "www.", "https://", "ftp://", "ftps://", "sftp://", "imap://" };
		static readonly Regex UrlMatchRegex = new Regex(@"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);  //this has been literally taken from the internet... because I'm dumb

		SuiBot_ChannelInstance ChannelInstance;
		ChatFilters Filters;
		ChatFilterUsersDB UserDB;

		public ChatFiltering(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
			Filters = ChatFilters.Load(ChannelInstance.Channel);
			UserDB = ChatFilterUsersDB.Load(ChannelInstance.Channel);
		}

		public void Dispose()
		{
			UserDB.Save();
			Filters = null;
			UserDB = null;
			ChannelInstance = null;
		}

		public void DoWork(ChatMessage lastMassage)
		{
			if (lastMassage.UserRole <= Role.Mod)
			{
				lastMassage.Message = lastMassage.Message.StripSingleWord();
				if (lastMassage.Message.StartsWithWordLazy("add"))
				{
					lastMassage.Message = lastMassage.Message.StripSingleWord();
					if (lastMassage.Message.StartsWithWordLazy("purge"))
					{
						AddFilter(ChatFilters.FilterType.Purge, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("timeout"))
					{
						AddFilter(ChatFilters.FilterType.Timeout, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("ban"))
					{
						AddFilter(ChatFilters.FilterType.Ban, lastMassage);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter add messages should be followed by purge / timeout / ban");
				}
				else if (lastMassage.Message.StartsWithWordLazy(new string[] { "remove", "delete" }))
				{
					lastMassage.Message = lastMassage.Message.StripSingleWord();
					if (lastMassage.Message.StartsWithWordLazy("purge"))
					{
						RemoveFilter(ChatFilters.FilterType.Purge, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("timeout"))
					{
						RemoveFilter(ChatFilters.FilterType.Timeout, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("ban"))
					{
						RemoveFilter(ChatFilters.FilterType.Ban, lastMassage);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter remove messages should be followed by purge / timeout / ban and then filter ID.");
				}
				else if (lastMassage.Message.StartsWithWordLazy(new string[] { "search", "find" }))
				{
					lastMassage.Message = lastMassage.Message.StripSingleWord();
					if (lastMassage.Message.StartsWithWordLazy("purge"))
					{
						SearchFilter(ChatFilters.FilterType.Purge, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("timeout"))
					{
						SearchFilter(ChatFilters.FilterType.Timeout, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("ban"))
					{
						SearchFilter(ChatFilters.FilterType.Ban, lastMassage);
					}
					else if (lastMassage.Message.StartsWith("lookup"))
					{
						SearchFilter(null, lastMassage);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter search messages should be followed by purge / timeout / ban and then ID or lookup and then message.");
				}
				else if (lastMassage.Message.StartsWithWordLazy("update"))
				{
					lastMassage.Message = lastMassage.Message.StripSingleWord();
					if (lastMassage.Message.StartsWithWordLazy("purge"))
					{
						UpdateFilter(ChatFilters.FilterType.Purge, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("timeout"))
					{
						UpdateFilter(ChatFilters.FilterType.Timeout, lastMassage);
					}
					else if (lastMassage.Message.StartsWithWordLazy("ban"))
					{
						UpdateFilter(ChatFilters.FilterType.Ban, lastMassage);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter update messages should be followed by purge / timeout / ban, then ID or Last and then parsable information.");
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. ChatFilter messages should be followed by add / remove / find / update.");
			}

		}

		private void SearchFilter(ChatFilters.FilterType? filterType, ChatMessage lastMassage)
		{
			lastMassage.Message = lastMassage.Message.StripSingleWord();

			if (filterType == null)
			{
				//Lookup section
				return;
			}
			else
			{
				//Id find section
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
						case ChatFilters.FilterType.Purge:
							{
								if (Filters.PurgeFilters.Count > 0)
								{
									var filter = Filters.PurgeFilters.Last();
									ChannelInstance.SendChatMessageResponse(lastMassage, "Purge Filter #" + (Filters.PurgeFilters.Count - 1).ToString() + ": " + filter.Syntax);
								}
								else
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters definied.");
							}
							break;
						case ChatFilters.FilterType.Timeout:
							{
								if (Filters.TimeOutFilter.Count > 0)
								{
									var filter = Filters.TimeOutFilter.Last();
									ChannelInstance.SendChatMessageResponse(lastMassage, "Purge Filter #" + (Filters.TimeOutFilter.Count - 1).ToString() + ": " + filter.Syntax);
								}
								else
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters definied.");
							}
							break;
						case ChatFilters.FilterType.Ban:
							{
								if (Filters.BanFilters.Count > 0)
								{
									var filter = Filters.BanFilters.Last();
									ChannelInstance.SendChatMessageResponse(lastMassage, "Purge Filter #" + (Filters.BanFilters.Count - 1).ToString() + ": " + filter.Syntax);
								}
								else
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no ban filters definied.");
							}
							break;
					}
				}
				else
				{
					if (int.TryParse(lastMassage.Message, out int id))
					{
						if (id < 0)
						{
							ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be negative!");
						}
						else
						{
							switch (filterType)
							{
								case ChatFilters.FilterType.Purge:
									{
										if (id >= Filters.PurgeFilters.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Purge Filters ({0})", Filters.PurgeFilters.Count - 1));
										else
										{
											var filter = Filters.PurgeFilters[id];
											ChannelInstance.SendChatMessageResponse(lastMassage, "Filter: " + filter.Syntax);
										}
									}
									break;
								case ChatFilters.FilterType.Timeout:
									{
										if (id >= Filters.TimeOutFilter.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Timeout Filters ({0})", Filters.TimeOutFilter.Count - 1));
										else
										{
											var filter = Filters.TimeOutFilter[id];
											ChannelInstance.SendChatMessageResponse(lastMassage, "Filter: " + filter.Syntax);
										}
									}
									break;
								case ChatFilters.FilterType.Ban:
									{
										if (id >= Filters.BanFilters.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Ban Filters ({0})", Filters.BanFilters.Count - 1));
										else
										{
											var filter = Filters.BanFilters[id];
											ChannelInstance.SendChatMessageResponse(lastMassage, "Filter: " + filter.Syntax);
										}
									}
									break;
							}
						}
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Failed to parse ID.");
				}
			}

		}

		/// <summary>
		/// Adds a new filter.
		/// </summary>
		/// <param name="filterType">Type of filter to be added based on FilterType enum.</param>
		/// <param name="lastMassage">Message to get the filter from.</param>
		private void AddFilter(ChatFilters.FilterType filterType, ChatMessage lastMassage)
		{
			lastMassage.Message = lastMassage.Message.StripSingleWord();
			if (lastMassage.Message == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMassage, "Filter can not be empty!");
				return;
			}

			//This is important, since we don't want regexes that fail to compile in memory
			try
			{
				Regex testRegex = new Regex(lastMassage.Message);
			}
			catch
			{
				ChannelInstance.SendChatMessageResponse(lastMassage, "Failed to compile regex!");
				return;
			}

			switch (filterType)
			{
				case ChatFilters.FilterType.Purge:
					{
						var newFilter = lastMassage.Message;
						Filters.PurgeFilters.Add(new ChatFilter(newFilter, "", 1));
						Filters.Save();
						ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update purge last Response:\"Custom response\"");
					}
					break;
				case ChatFilters.FilterType.Timeout:
					{
						var newFilter = lastMassage.Message;
						Filters.TimeOutFilter.Add(new ChatFilter(newFilter, "", 1));
						Filters.Save();
						ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update timeout last Response:\"Custom response\", Duration:\"Lenght\"");
					}
					break;
				case ChatFilters.FilterType.Ban:
					{
						var newFilter = lastMassage.Message;
						Filters.BanFilters.Add(new ChatFilter(newFilter, "", 1));
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
		private void UpdateFilter(ChatFilters.FilterType filterType, ChatMessage lastMassage)
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
					case ChatFilters.FilterType.Purge:
						{
							if (Filters.PurgeFilters.Count > 0)
							{
								AdvancedFilters(lastMassage.Message, out string Response, out _);
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
					case ChatFilters.FilterType.Timeout:
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
					case ChatFilters.FilterType.Ban:
						{
							if (Filters.BanFilters.Count > 0)
							{
								AdvancedFilters(lastMassage.Message, out string Response, out _);
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
					var idAsString = lastMassage.Message.Split(new char[] { ' ' });
					if (int.TryParse(idAsString[0], out int id))
					{
						if (id < 0)
						{
							ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be negative!");
						}
						else
						{
							switch (filterType)
							{
								case ChatFilters.FilterType.Purge:
									{
										if (id >= Filters.PurgeFilters.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Purge Filters ({0})", Filters.PurgeFilters.Count - 1));
										else
										{
											AdvancedFilters(lastMassage.Message, out string responseText, out _);
											var filter = Filters.PurgeFilters[id];
											filter.Duration = 1;
											filter.Response = responseText;
											Filters.PurgeFilters[id] = filter;
											Filters.Save();
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Updated purge filter {0} with a response: {1}", id, responseText));
										}
									}
									break;
								case ChatFilters.FilterType.Timeout:
									{
										if (id >= Filters.TimeOutFilter.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Timeout Filters ({0})", Filters.TimeOutFilter.Count - 1));
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
								case ChatFilters.FilterType.Ban:
									{
										if (id >= Filters.BanFilters.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a current last ID of Ban Filters ({0})", Filters.BanFilters.Count - 1));
										else
										{
											AdvancedFilters(lastMassage.Message, out string responseText, out _);
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
					{
						ChannelInstance.SendChatMessageResponse(lastMassage, "Failed to parse ID.");
						return;
					}
				}
				else
				{
					ChannelInstance.SendChatMessageResponse(lastMassage, "No ID provided. Use !chatfilter update <type> <id> response:\"Custom response\", [duration:\"Time\"]");
					return;
				}


			}
		}

		/// <summary>
		/// Removes the filter using a given ID.
		/// </summary>
		/// <param name="filterType">Type of filter to be removed based on FilterType enum.</param>
		/// <param name="lastMassage">Message from which to get id from.</param>
		private void RemoveFilter(ChatFilters.FilterType filterType, ChatMessage lastMassage)
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
					case ChatFilters.FilterType.Purge:
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
					case ChatFilters.FilterType.Timeout:
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
					case ChatFilters.FilterType.Ban:
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
				if (int.TryParse(lastMassage.Message, out int id))
				{
					if (id < 0)
					{
						ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be negative!");
					}
					else
					{
						switch (filterType)
						{
							case ChatFilters.FilterType.Purge:
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
							case ChatFilters.FilterType.Timeout:
								{
									if (id >= Filters.TimeOutFilter.Count)
										ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.TimeOutFilter.Count - 1));
									else
									{
										Filters.TimeOutFilter.RemoveAt(id);
										Filters.Save();
										ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Removed timeout filter {0}", id));
									}
								}
								break;
							case ChatFilters.FilterType.Ban:
								{
									if (id >= Filters.BanFilters.Count)
										ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("ID was higher than a total number of Purge Filters ({0})", Filters.BanFilters.Count - 1));
									else
									{
										Filters.BanFilters.RemoveAt(id);
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
		}

		/// <summary>
		/// Function that seperates response text and duration lenght from a message (Filter syntax)
		/// </summary>
		/// <param name="message">Message from which to seperate response and duration from.</param>
		/// <param name="response">Response text (String.Empty if none provided).</param>
		/// <param name="lenght">Duration lenght (1 if none provided of error)</param>
		private void AdvancedFilters(string message, out string response, out uint lenght)
		{
			MatchCollection responseMatches = Regex.Matches(message, RegexFindResponse, RegexOptions.IgnoreCase);
			if (responseMatches.Count > 0)
			{
				response = responseMatches[0].Value.Remove(0, "response:".Length).Trim(new char[] { ' ', '\"' });
			}
			else
				response = "";

			MatchCollection lenghtMatches = Regex.Matches(message, RegexFindTimeoutLenght, RegexOptions.IgnoreCase);
			if (lenghtMatches.Count > 0)
			{
				var timeStripped = lenghtMatches[0].Value.Remove(0, "duration:".Length).Trim(new char[] { ' ', '\"' });
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
			if (ChannelInstance.ConfigInstance.FilterLinks && !UserDB.CanPostLinks(lastMassage.Username))
			{
				if (ContainsLink(lastMassage))
				{
					ChannelInstance.RemoveUserMessage(lastMassage);

					UserDB.ResetCounter(lastMassage.Username);
					if (!lastMassage.IsFirstMessage)
						ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Please make sure you have more than 3 messages in the chat before posting a link."));
					return true;
				}
			}

			int id = 0;
			foreach (ChatFilter filter in Filters.PurgeFilters)
			{
				try
				{
					if (filter.CompiledSyntax.IsMatch(lastMassage.Message))
					{
						ChannelInstance.RemoveUserMessage(lastMassage);
						ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Removed message: {0} (filterID: {1})", filter.Response, id));
						return true;
					}
				}
				catch (Exception ex)
				{
					ErrorLogging.WriteLine("Regex error: " + ex);
				}

				id++;
			}

			id = 0;
			foreach (ChatFilter filter in Filters.TimeOutFilter)
			{
				try
				{
					if (filter.CompiledSyntax.IsMatch(lastMassage.Message))
					{
						ChannelInstance.UserTimetout(lastMassage, filter.Duration, string.Format("{0} (filterID: {1})", filter.Response, id));
						return true;
					}
				}
				catch (Exception ex)
				{
					ErrorLogging.WriteLine("Regex error: " + ex);
				}
			}

			id = 0;
			foreach (ChatFilter filter in Filters.BanFilters)
			{
				try
				{
					if (filter.CompiledSyntax.IsMatch(lastMassage.Message))
					{
						ChannelInstance.UserBan(lastMassage, string.Format("{0} (filterID: {1})", filter.Response, id));

						return true;
					}
					id++;
				}
				catch (Exception ex)
				{
					ErrorLogging.WriteLine("Regex error: " + ex);
				}
			}
			return false;
		}

		//Checks if message contains a link
		private bool ContainsLink(ChatMessage lastMassage)
		{
			string[] words;
			if (lastMassage.Message.Contains(" "))
				words = lastMassage.Message.Split(' ');
			else
				words = new string[] { lastMassage.Message };

			foreach (string word in words)
			{
				if (UrlMarks.Any(x => word.StartsWithLazy(x)))
					return true;

				if (UrlMatchRegex.IsMatch(word))
					return true;
			}

			return false;
		}
	}
}

using SuiBot_Core.API.EventSub;
using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static SuiBot_Core.API.EventSub.ES_ChatMessage;

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

		public void DoWork(ES_ChatMessage lastMassage)
		{
			if (lastMassage.UserRole <= Role.Mod)
			{
				var msg = lastMassage.message.text.StripSingleWord();
				if (msg.StartsWithWordLazy("add"))
				{
					msg = msg.StripSingleWord();
					if (msg.StartsWithWordLazy("purge"))
					{
						AddFilter(ChatFilters.FilterType.Purge, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("timeout"))
					{
						AddFilter(ChatFilters.FilterType.Timeout, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("ban"))
					{
						AddFilter(ChatFilters.FilterType.Ban, lastMassage, msg);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter add messages should be followed by purge / timeout / ban");
				}
				else if (msg.StartsWithWordLazy(new string[] { "remove", "delete" }))
				{
					msg = msg.StripSingleWord();
					if (msg.StartsWithWordLazy("purge"))
					{
						RemoveFilter(ChatFilters.FilterType.Purge, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("timeout"))
					{
						RemoveFilter(ChatFilters.FilterType.Timeout, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("ban"))
					{
						RemoveFilter(ChatFilters.FilterType.Ban, lastMassage, msg);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter remove messages should be followed by purge / timeout / ban and then filter ID.");
				}
				else if (msg.StartsWithWordLazy(new string[] { "search", "find" }))
				{
					msg = msg.StripSingleWord();
					if (msg.StartsWithWordLazy("purge"))
					{
						SearchFilter(ChatFilters.FilterType.Purge, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("timeout"))
					{
						SearchFilter(ChatFilters.FilterType.Timeout, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("ban"))
					{
						SearchFilter(ChatFilters.FilterType.Ban, lastMassage, msg);
					}
					else if (msg.StartsWith("lookup"))
					{
						SearchFilter(null, lastMassage, msg);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter search messages should be followed by purge / timeout / ban and then ID or lookup and then message.");
				}
				else if (msg.StartsWithWordLazy("update"))
				{
					msg = msg.StripSingleWord();
					if (msg.StartsWithWordLazy("purge"))
					{
						UpdateFilter(ChatFilters.FilterType.Purge, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("timeout"))
					{
						UpdateFilter(ChatFilters.FilterType.Timeout, lastMassage, msg);
					}
					else if (msg.StartsWithWordLazy("ban"))
					{
						UpdateFilter(ChatFilters.FilterType.Ban, lastMassage, msg);
					}
					else
						ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. Chatfilter update messages should be followed by purge / timeout / ban, then ID or Last and then parsable information.");
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMassage, "Invalid command. ChatFilter messages should be followed by add / remove / find / update.");
			}

		}

		private void SearchFilter(ChatFilters.FilterType? filterType, ES_ChatMessage lastMassage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();

			if (filterType == null)
			{
				//Lookup section
				return;
			}
			else
			{
				//Id find section
				if (strippedMessage == "")
				{
					ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be empty!");
					return;
				}
				else if (strippedMessage.StartsWithLazy("last"))
				{
					strippedMessage = strippedMessage.StripSingleWord();

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
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters defined.");
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
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters defined.");
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
									ChannelInstance.SendChatMessageResponse(lastMassage, "There are no ban filters defined.");
							}
							break;
					}
				}
				else
				{
					if (int.TryParse(strippedMessage, out int id))
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
		private void AddFilter(ChatFilters.FilterType filterType, ES_ChatMessage lastMassage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMassage, "Filter can not be empty!");
				return;
			}

			//This is important, since we don't want regexes that fail to compile in memory
			try
			{
				Regex testRegex = new Regex(strippedMessage);
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
						var newFilter = strippedMessage;
						Filters.PurgeFilters.Add(new ChatFilter(newFilter, "", 1));
						Filters.Save();
						ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update purge last Response:\"Custom response\"");
					}
					break;
				case ChatFilters.FilterType.Timeout:
					{
						var newFilter = strippedMessage;
						Filters.TimeOutFilter.Add(new ChatFilter(newFilter, "", 1));
						Filters.Save();
						ChannelInstance.SendChatMessageResponse(lastMassage, "Added! You can update it further using command: !chatfilter update timeout last Response:\"Custom response\", Duration:\"Length\"");
					}
					break;
				case ChatFilters.FilterType.Ban:
					{
						var newFilter = strippedMessage;
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
		private void UpdateFilter(ChatFilters.FilterType filterType, ES_ChatMessage lastMassage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be empty!");
				return;
			}
			else if (strippedMessage.StartsWithLazy("last"))
			{
				strippedMessage = strippedMessage.StripSingleWord();

				switch (filterType)
				{
					case ChatFilters.FilterType.Purge:
						{
							if (Filters.PurgeFilters.Count > 0)
							{
								AdvancedFilters(strippedMessage, out string Response, out _);
								var filter = Filters.PurgeFilters.Last();
								filter.Duration = 1;
								filter.Response = Response;
								Filters.PurgeFilters[Filters.PurgeFilters.Count - 1] = filter;
								Filters.Save();
								ChannelInstance.SendChatMessageResponse(lastMassage, $"Updated purge filter with response: {Response}");
							}
							else
								ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters defined.");
						}
						break;
					case ChatFilters.FilterType.Timeout:
						{
							if (Filters.TimeOutFilter.Count > 0)
							{
								AdvancedFilters(strippedMessage, out string Response, out uint Length);
								var filter = Filters.TimeOutFilter.Last();
								filter.Duration = Length;
								filter.Response = Response;
								Filters.TimeOutFilter[Filters.TimeOutFilter.Count - 1] = filter;
								Filters.Save();
								ChannelInstance.SendChatMessageResponse(lastMassage, $"Updated timeout filter with duration of {Length} seconds and response: {Response}");
							}
							else
								ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters defined.");
						}
						break;
					case ChatFilters.FilterType.Ban:
						{
							if (Filters.BanFilters.Count > 0)
							{
								AdvancedFilters(strippedMessage, out string Response, out _);
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
				if (strippedMessage.Contains(" "))
				{
					var idAsString = strippedMessage.Split(new char[] { ' ' });
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
											ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a current last ID of Purge Filters ({Filters.PurgeFilters.Count - 1})");
										else
										{
											AdvancedFilters(strippedMessage, out string responseText, out _);
											var filter = Filters.PurgeFilters[id];
											filter.Duration = 1;
											filter.Response = responseText;
											Filters.PurgeFilters[id] = filter;
											Filters.Save();
											ChannelInstance.SendChatMessageResponse(lastMassage, $"Updated purge filter {id} with a response: {responseText}");
										}
									}
									break;
								case ChatFilters.FilterType.Timeout:
									{
										if (id >= Filters.TimeOutFilter.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a current last ID of Timeout Filters ({Filters.TimeOutFilter.Count - 1})");
										else
										{
											AdvancedFilters(strippedMessage, out string responseText, out uint length);
											var filter = Filters.TimeOutFilter[id];
											filter.Duration = length;
											filter.Response = responseText;
											Filters.TimeOutFilter[id] = filter;
											Filters.Save();
											ChannelInstance.SendChatMessageResponse(lastMassage, $"Updated timeout filter {id} with duration of {length} and response: {responseText}");
										}
									}
									break;
								case ChatFilters.FilterType.Ban:
									{
										if (id >= Filters.BanFilters.Count)
											ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a current last ID of Ban Filters ({Filters.BanFilters.Count - 1})");
										else
										{
											AdvancedFilters(strippedMessage, out string responseText, out _);
											var filter = Filters.BanFilters[id];
											filter.Duration = 1;
											filter.Response = responseText;
											Filters.BanFilters[id] = filter;
											Filters.Save();
											ChannelInstance.SendChatMessageResponse(lastMassage, $"Updated ban filter {id} with a response: {responseText}");
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
		private void RemoveFilter(ChatFilters.FilterType filterType, ES_ChatMessage lastMassage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMassage, "ID can not be empty!");
				return;
			}
			else if (strippedMessage.StartsWithLazy("last"))
			{
				strippedMessage = strippedMessage.StripSingleWord();

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
								ChannelInstance.SendChatMessageResponse(lastMassage, "There are no purge filters defined.");
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
								ChannelInstance.SendChatMessageResponse(lastMassage, "There are no timeout filters defined.");
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
								ChannelInstance.SendChatMessageResponse(lastMassage, "There are no ban filters defined.");
						}
						break;
				}
			}
			else
			{
				if (int.TryParse(strippedMessage, out int id))
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
										ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a total number of Purge Filters ({Filters.PurgeFilters.Count - 1})");
									else
									{
										Filters.PurgeFilters.RemoveAt(id);
										Filters.Save();
										ChannelInstance.SendChatMessageResponse(lastMassage, $"Removed purge filter {id}");
									}
								}
								break;
							case ChatFilters.FilterType.Timeout:
								{
									if (id >= Filters.TimeOutFilter.Count)
										ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a total number of Purge Filters ({Filters.TimeOutFilter.Count - 1})");
									else
									{
										Filters.TimeOutFilter.RemoveAt(id);
										Filters.Save();
										ChannelInstance.SendChatMessageResponse(lastMassage, $"Removed timeout filter {id}");
									}
								}
								break;
							case ChatFilters.FilterType.Ban:
								{
									if (id >= Filters.BanFilters.Count)
										ChannelInstance.SendChatMessageResponse(lastMassage, $"ID was higher than a total number of Purge Filters ({Filters.BanFilters.Count - 1})");
									else
									{
										Filters.BanFilters.RemoveAt(id);
										Filters.Save();
										ChannelInstance.SendChatMessageResponse(lastMassage, $"Removed ban filter {id}");
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
		/// Function that separates response text and duration length from a message (Filter syntax)
		/// </summary>
		/// <param name="message">Message from which to separate response and duration from.</param>
		/// <param name="response">Response text (String.Empty if none provided).</param>
		/// <param name="length">Duration lenght (1 if none provided of error)</param>
		private void AdvancedFilters(string message, out string response, out uint length)
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
				if (!uint.TryParse(timeStripped, out length))
					length = 1;
			}
			else
				length = 1;
		}

		/// <summary>
		/// Iterates through all filters and takes action if needed
		/// </summary>
		/// <param name="lastMassage">Message to iterate through</param>
		/// <returns>Boolean value indicating whatever an action needed to be taken or not.</returns>
		public bool FilterOutMessages(ES_ChatMessage lastMassage)
		{
			if (ChannelInstance.ConfigInstance.FilterLinks && !UserDB.CanPostLinks(lastMassage.chatter_user_login))
			{
				if (ContainsLink(lastMassage))
				{
					ChannelInstance.RemoveUserMessage(lastMassage);

					UserDB.ResetCounter(lastMassage.chatter_user_login);
/*					if (!lastMassage.IsFirstMessage)
						ChannelInstance.SendChatMessageResponse(lastMassage, string.Format("Please make sure you have more than 3 messages in the chat before posting a link."));*/
					return true;
				}
			}

			int id = 0;
			foreach (ChatFilter filter in Filters.PurgeFilters)
			{
				try
				{
					if (filter.CompiledSyntax.IsMatch(lastMassage.message.text))
					{
						ChannelInstance.RemoveUserMessage(lastMassage);
						ChannelInstance.SendChatMessageResponse(lastMassage, $"Removed message: {filter.Response} (filterID: {id})");
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
					if (filter.CompiledSyntax.IsMatch(lastMassage.message.text))
					{
						ChannelInstance.UserTimeout(lastMassage, filter.Duration, $"{filter.Response} (filterID: {id})");
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
					if (filter.CompiledSyntax.IsMatch(lastMassage.message.text))
					{
						ChannelInstance.UserBan(lastMassage, $"{filter.Response} (filterID: {id})");

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
		private bool ContainsLink(ES_ChatMessage lastMassage)
		{
			string[] words;
			if (lastMassage.message.text.Contains(" "))
				words = lastMassage.message.text.Split(' ');
			else
				words = new string[] { lastMassage.message.text };

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

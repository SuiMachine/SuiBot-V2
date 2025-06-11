using SpeedrunComSharp;
using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SuiBot_Core.Components
{
	public class Leaderboards
	{
		public const string PROXYFILENAMESURL = "https://raw.githubusercontent.com/SuiMachine/SuiBot-V2/master/Release/Bot/SpeedrunProxyNames.xml";
		public const string PROXYNAMESFILE = "Bot/SpeedrunProxyNames.xml";
		private SuiBot_ChannelInstance m_ChannelInstance;
		const string RegexSyntaxGame = "game(:|=)\".+?\"";
		const string RegexSyntaxCategory = "category(:|=)\".+?\"";
		const string RegexSyntaxLevel = "level(:|=)\".+?\"";
		//const string RegexSubCategoryShort = "subcategory(:|=)\"(.+)?\"(:|=)\".+?\"";
		//const string RegexSubCategoryLong = "subcategory(:|=)\"(.+)?\"(:|=)\".+?\"";
		//const string RegexVariables = "variable(:|=)\"(.+)?\"(:|=)\".+?\"";
		public bool GameOverride { get; set; }
		public string CurrentGame { get; set; }
		public string LevelOverride { get; set; }
		public string CategoryOverride { get; set; }
		public string PreferredCategory { get; set; }
		public Dictionary<string, string> SubcategoriesOverride { get; set; }
		public Dictionary<string, string> VariablesOverride { get; set; }

		private string SpeedrunUsername => m_ChannelInstance.ConfigInstance.LeaderboardsUsername;
		public bool LastUpdateSuccessful { get; private set; }


		#region ProxyNamesDeclaration
		static Dictionary<string, ProxyNameInMemory> PROXYNAMES = null;

		private static void GetProxyName(ref string lookUpGame, ref string lookUpCategory, ref string lookUpLevel)
		{
			lookUpGame = lookUpGame.ToLower().Trim();
			if (PROXYNAMES.TryGetValue(lookUpGame, out var proxy))
			{
				lookUpGame = proxy.ProxyName;
				lookUpCategory = proxy.Category;
			}
		}
		#endregion

		public Leaderboards(SuiBot_ChannelInstance channelInstance)
		{
			this.m_ChannelInstance = channelInstance;
			GameOverride = false;
			CurrentGame = "";
			LevelOverride = "";
			CategoryOverride = "";
			PreferredCategory = "";
			SubcategoriesOverride = new Dictionary<string, string>();


			if (PROXYNAMES == null)
			{
				PROXYNAMES = new Dictionary<string, ProxyNameInMemory>();
				if (File.Exists(PROXYNAMESFILE))
				{
					PROXYNAMES = LoadProxyNamesFromFile(PROXYNAMESFILE);
				}

				if (this.m_ChannelInstance.ConfigInstance.LeaderboardsUpdateProxyNames)
					UpdateProxyNamesAsync();
			}

		}

		private void UpdateProxyNamesAsync()
		{
			try
			{
				WebClient wbClient = new WebClient();
				wbClient.DownloadFileCompleted += WbClient_DownloadFileCompleted;
				string tempFile = "TempProxyNames.xml";
				if (File.Exists(tempFile))
					File.Delete(tempFile);

				wbClient.DownloadFileAsync(new Uri(PROXYFILENAMESURL), tempFile);
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine($"Error checking Proxyname XML on Github {e.Message}");
			}
		}

		private void WbClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				ErrorLogging.WriteLine("Failed to download new proxy names file for leaderboard: " + e.Error.ToString());
				return;
			}

			if (e.Cancelled)
				return;

			var temp = LoadProxyNamesFromFile("TempProxyNames.xml", true);
			if (temp.Count > 0)
			{
				if (File.Exists(PROXYNAMESFILE))
					File.Delete(PROXYNAMESFILE);
				File.Move("TempProxyNames.xml", PROXYNAMESFILE);
				PROXYNAMES = temp;
			}
			else
			{
				ErrorLogging.WriteLine("Can't serialize new proxy names file");
				File.Delete("TempProxyNames.xml");
			}
		}

		private Dictionary<string, ProxyNameInMemory> LoadProxyNamesFromFile(string filePath, bool hasToLoad = false)
		{
			Dictionary<string, ProxyNameInMemory> newProxyNamesDictionary = new Dictionary<string, ProxyNameInMemory>();

			StreamReader sr = null;
			try
			{
				XmlSerializer serialiser = new XmlSerializer(typeof(ProxyNameInFile[]));
				sr = new StreamReader(filePath);
				var newObj = (ProxyNameInFile[])serialiser.Deserialize(sr);

				newProxyNamesDictionary = newObj.ToDictionary(x => x.Game.ToLower(), x => (ProxyNameInMemory)x);
			}
			finally
			{
				if (sr != null)
					sr.Close();
			}

			if (newProxyNamesDictionary.Count == 0 && !hasToLoad)
				return PROXYNAMES;
			else
				return newProxyNamesDictionary;

		}

		public void SetPreferredCategory(string StreamTitle, bool isAfterFirstUpdate, bool vocal)
		{
			var currentTitleLC = StreamTitle.ToLower();

			try
			{
				var srSearch = new SpeedrunComClient();
				var srGame = srSearch.Games.SearchGame(CurrentGame);

				if (srGame != null)
				{
					//Go from longest category titles to shortest (so for example we respect any% NG+, despite it having higher ID
					var fullGameCategories = srGame.FullGameCategories.OrderByDescending(x => x.Name.Length);
					foreach (var category in fullGameCategories)
					{
						if (currentTitleLC.Contains(category.Name.ToLower()))
						{
							if (PreferredCategory != category.Name || vocal)
							{
								PreferredCategory = category.Name;
								LastUpdateSuccessful = true;
								if (vocal || isAfterFirstUpdate)
									m_ChannelInstance.SendChatMessage($"Set leaderboards category to: \"{PreferredCategory}\" based on stream title");
							}
							return;
						}
					}
					PreferredCategory = "";
					LastUpdateSuccessful = true;
					if (vocal || isAfterFirstUpdate)
						m_ChannelInstance.SendChatMessage("Haven't found the category in stream title.");
					return;
				}
				LastUpdateSuccessful = true;
				if (vocal)
					m_ChannelInstance.SendChatMessage("Haven't found the game on speedrun.com. !leaderboards game %GAME TITLE% might be used to force the game.");
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine("Error setting preferred category: " + e.Message);
				LastUpdateSuccessful = false;
			}
		}

		public void DoModWork(ES_ChatMessage lastMessage)
		{
			var msg = lastMessage.message.text.StripSingleWord();

			if (msg.StartsWithLazy("username"))
			{
				msg = msg.StripSingleWord();
				if (msg != "")
				{
					m_ChannelInstance.ConfigInstance.LeaderboardsUsername = msg;
					m_ChannelInstance.ConfigInstance.Save();
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Set Speedrun username to: " + SpeedrunUsername);
				}
				else
				{
					m_ChannelInstance.ConfigInstance.LeaderboardsUsername = msg;
					m_ChannelInstance.ConfigInstance.Save();
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Nulled out Speedrun username");
				}
			}
			else if (msg.StartsWithLazy("game"))
			{
				msg = msg.StripSingleWord();
				if (msg != "")
				{
					GameOverride = true;
					CurrentGame = msg;
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Set game override to: " + CurrentGame);
				}
				else
				{
					GameOverride = false;
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Disabled game override (game will be updated on next twitch status update).");
				}
			}
			else if (msg.StartsWithLazy("level"))
			{
				msg = msg.StripSingleWord();
				if (msg != "")
				{
					LevelOverride = msg;
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Set level to: " + LevelOverride);
				}
				else
				{
					LevelOverride = "";
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Disabled level override.");
				}
			}
			else if (msg.StartsWithLazy("category"))
			{
				msg = msg.StripSingleWord();
				if (msg != "")
				{
					CategoryOverride = msg;
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Set category to: " + CategoryOverride);
				}
				else
				{
					CategoryOverride = "";
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Disabled category override.");
				}
			}
			else if (msg.StartsWithLazy("subcategory"))
			{
				msg = msg.StripSingleWord().ToLower();
				if (msg != "")
				{
					//Precise separation
					if (msg.Contains(":"))
					{
						var split = msg.Split(new char[] { ':' }, 2);
						var key = split[0].Trim();
						var value = split[1].Trim();
						if (key != "")
						{
							if (SubcategoriesOverride.ContainsKey(split[0]))
							{
								if (value == "")
								{
									SubcategoriesOverride.Remove(split[0]);
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed subcategory key \"{key}\"");
								}
								else
								{
									SubcategoriesOverride[key] = value;
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Set the subcategory key \"{key}\" to \"{value}\"");
								}

							}
							else
							{
								if (value == "")
									m_ChannelInstance.SendChatMessageResponse(lastMessage, "Nothing was deleted as such key doesn't exist");
								else
								{
									SubcategoriesOverride.Add(key, value);
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Added new subcategory override with key \"{key}\" and value \"{value}\"");
								}
							}
						}
						else
						{
							m_ChannelInstance.SendChatMessageResponse(lastMessage, "Subcategory key can not be empty!");
						}

					}
					else  //Lazy seperation
					{
						if (SubcategoriesOverride.ContainsKey(msg))
							SubcategoriesOverride[msg] = "";
						else
							SubcategoriesOverride.Add(msg, "");

						m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Set the generic subcategory value to look for to \"{msg}\"");
					}
				}
				else
				{
					SubcategoriesOverride.Clear();
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Cleared up subcategory overrides.");
				}
			}
			else if (msg.StartsWithLazy("variable"))
			{
				msg = msg.StripSingleWord().ToLower();
				if (msg != "")
				{
					//Precise separation
					if (msg.Contains(":"))
					{
						var split = msg.Split(new char[] { ':' }, 2);
						var key = split[0].Trim();
						var value = split[1].Trim();
						if (key != "")
						{
							if (VariablesOverride.ContainsKey(split[0]))
							{
								if (value == "")
								{
									VariablesOverride.Remove(split[0]);
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed variable variable key \"{key}\"");
								}
								else
								{
									VariablesOverride[key] = value;
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Set the variable key \"{key}\" to \"{value}\"");
								}
							}
							else
							{
								if (value == "")
									m_ChannelInstance.SendChatMessageResponse(lastMessage, "Nothing was deleted as such key doesn't exist");
								else
								{
									VariablesOverride.Add(key, value);
									m_ChannelInstance.SendChatMessageResponse(lastMessage, $"Added new variable with key \"{key}\" and value \"{value}\"");
								}
							}
						}
						else
						{
							m_ChannelInstance.SendChatMessageResponse(lastMessage, "Variable key can not be empty!");
						}
					}
					else  //Doing lazy way would be stupid for variables
					{
						m_ChannelInstance.SendChatMessageResponse(lastMessage, "Setting variables requires key and value");
					}
				}
				else
				{
					VariablesOverride.Clear();
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "Cleared up variable overrides.");
				}
			}
		}

		public void DoWorkWR(ES_ChatMessage lastMessage)
		{
			var msg = lastMessage.message.text.StripSingleWord();

			if (msg == "")
			{
				var currentGame = CurrentGame;
				var category = "";
				var level = "";
				GetProxyName(ref currentGame, ref category, ref level);
				m_ChannelInstance.SendChatMessageResponse(lastMessage, GetWR(currentGame, true, category, level, null, null));
			}
			else
			{
				bool isCurrentGame = false;
				SeperateElements(msg, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out Dictionary<string, string> lookUpSubcategories, out Dictionary<string, string> lookUpVariables);

				//Sort out game name
				if (lookUpGame.ToLower() == "this")
				{
					lookUpGame = CurrentGame;
					isCurrentGame = true;
				}

				if (lookUpGame == "")
				{
					if (lookUpCategory == "" && lookUpLevel == "" && lookUpLevel == "" && lookUpSubcategories == null && lookUpVariables == null)
					{
						lookUpGame = msg;
					}
					else
					{
						lookUpGame = CurrentGame;
						isCurrentGame = true;
					}
				}

				if (lookUpCategory == "" && lookUpLevel == "")
					GetProxyName(ref lookUpGame, ref lookUpCategory, ref lookUpLevel);

				if (lookUpGame == "")
				{
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "No game name provided");
				}
				else
				{
					m_ChannelInstance.SendChatMessageResponse(lastMessage, GetWR(lookUpGame, isCurrentGame, lookUpCategory, lookUpLevel, lookUpSubcategories, lookUpVariables));
				}
			}
		}

		private string GetWR(string game, bool isCurrent, string category, string level, Dictionary<string, string> subcategories, Dictionary<string, string> variables)
		{
			try
			{
				SetParameters(ref isCurrent, ref category, ref level, ref subcategories, ref variables);

				if (game == "")
					return "No game provided";

				var srSearch = new SpeedrunComClient();
				var srGame = srSearch.Games.SearchGame(game);
				if (srGame == null)
					return "No game was found";
				else
				{
					//Levels generally have different category, so they need a seperate look up
					if (level != "")
					{
						Category srCategory;
						if (category == "")
							srCategory = srGame.LevelCategories.ElementAt(0);
						else
							srCategory = srGame.LevelCategories.FirstOrDefault(cat => cat.Name.ToLower().StartsWith(category.ToLower()));

						if (srCategory == null)
							return "No category was found";

						var srLevel = srGame.Levels.FirstOrDefault(lvl => lvl.Name.ToLower().StartsWith(level.ToLower()));
						if (srLevel == null)
							return "No level was found";

						//Haven't tested this for levels
						List<VariableValue> lookVariable = GetVariablesForLevel(ref subcategories, ref variables, ref srCategory, ref srLevel);

						var leaderboard = srSearch.Leaderboards.GetLeaderboardForLevel(srGame.ID, srLevel.ID, srCategory.ID, variableFilters: lookVariable);

						if (leaderboard.Records.Count > 0)
						{
							var record = leaderboard.Records[0];
							string runners = "";
							if (record.Players.Count > 1)
							{
								for (int i = 0; i < record.Players.Count - 1; i++)
								{
									runners += record.Players[i].Name + ", ";
								}
								runners += record.Players[record.Players.Count - 1].Name;
							}
							else
								runners = record.Player.Name;

							return string.Format("{0} - Level: {1} WR ({2}) is {3} by {4} - {5}",
								srGame.Name, srLevel.Name,
								srCategory.Name,
								record.Times.Primary.ToString(),
								runners,
								record.WebLink.ToString()
								);
						}
						else
							return $"No records were found! - {leaderboard.WebLink}";
					}
					//Full-game run
					else
					{
						Category srCategory;
						if (category == "")
							srCategory = srGame.FullGameCategories.ElementAt(0);
						else
							srCategory = srGame.FullGameCategories.FirstOrDefault(cat => cat.Name.ToLower().StartsWith(category.ToLower()));

						if (srCategory == null)
							return "No category was found";

						List<VariableValue> lookVariable = GetVariablesForFullGameCategory(ref subcategories, ref variables, ref srCategory);

						var leaderboard = srSearch.Leaderboards.GetLeaderboardForFullGameCategory(srGame.ID, srCategory.ID, variableFilters: lookVariable);

						if (leaderboard.Records.Count > 0)
						{
							var record = leaderboard.Records[0];
							string runners = "";
							if (record.Players.Count > 1)
							{
								for (int i = 0; i < record.Players.Count - 1; i++)
								{
									runners += record.Players[i].Name + ", ";
								}
								runners += record.Players[record.Players.Count - 1].Name;
							}
							else
								runners = record.Player.Name;

							return $"{srGame.Name} ({srCategory.Name}) record is {record.Times.Primary} by {runners} - {record.WebLink}";
						}
						else
							return "Leaderboard doesn't have any records! " + leaderboard.WebLink;
						;
					}
				}
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine(e.ToString());
				return "Error looking for a game on speedrun.com.";
			}
		}

		internal void DoWorkPB(ES_ChatMessage lastMessage)
		{
			var msg = lastMessage.message.text.StripSingleWord();

			if (msg == "")
			{
				m_ChannelInstance.SendChatMessageResponse(lastMessage, GetPB(CurrentGame, true, "", "", null, null));
			}
			else
			{
				bool isCurrentGame = false;
				SeperateElements(msg, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out Dictionary<string, string> lookUpSubcategories, out Dictionary<string, string> lookUpVariables);

				//Sort out game name
				if (lookUpGame.ToLower() == "this")
				{
					lookUpGame = CurrentGame;
					isCurrentGame = true;
				}

				if (lookUpGame == "")
				{
					if (lookUpCategory == "" && lookUpLevel == "" && lookUpLevel == "" && lookUpSubcategories == null && lookUpVariables == null)
					{
						lookUpGame = msg;
					}
				}

				if (lookUpCategory == "" && lookUpLevel == "")
					GetProxyName(ref lookUpGame, ref lookUpCategory, ref lookUpLevel);

				if (lookUpGame == "")
				{
					m_ChannelInstance.SendChatMessageResponse(lastMessage, "No game name provided");
				}
				else
				{
					m_ChannelInstance.SendChatMessageResponse(lastMessage, GetPB(lookUpGame, isCurrentGame, lookUpCategory, lookUpLevel, lookUpSubcategories, lookUpVariables));
				}
			}
		}

		private string GetPB(string game, bool isCurrentGame, string category, string level, Dictionary<string, string> subcategories, Dictionary<string, string> variables)
		{
			try
			{
				SetParameters(ref isCurrentGame, ref category, ref level, ref subcategories, ref variables);

				var srSearch = new SpeedrunComClient();
				var srGame = srSearch.Games.SearchGame(game);

				if (srGame == null)
					return "No game was found";
				else
				{
					//Levels generally have different category, so they need a seperate look up
					if (level != "")
					{
						Category srCategory;
						if (category == "")
							srCategory = srGame.LevelCategories.ElementAt(0);
						else
							srCategory = srGame.LevelCategories.FirstOrDefault(cat => cat.Name.ToLower().StartsWith(category.ToLower()));

						if (srCategory == null)
							return "No category was found";

						var srLevel = srGame.Levels.FirstOrDefault(lvl => lvl.Name.ToLower().StartsWith(level.ToLower()));
						if (srLevel == null)
							return "No level was found";

						var pbs = srSearch.Users.GetPersonalBests(SpeedrunUsername, gameId: srGame.ID);

						var levelPBs = pbs.Where(x => x.LevelID == srLevel.ID);
						if (levelPBs.Count() > 0)
						{
							var SRVariables = GetVariablesForLevel(ref subcategories, ref variables, ref srCategory, ref srLevel);

							var bestPBsInCategory = levelPBs.Where(x => x.Category.ID == srCategory.ID);
							if (SRVariables != null)
							{
								for (int i = 0; i < SRVariables.Count; i++)
								{
									bestPBsInCategory = bestPBsInCategory.Where(x => x.VariableValues.Contains(SRVariables[i]));
								}
							}

							var bestPBInCategory = bestPBsInCategory.FirstOrDefault();

							if (bestPBInCategory != null)
								return $"Streamer\'s PB for {srGame.Name} ({srCategory.Name}) in level \"{srLevel.Name}\" is {bestPBInCategory.Times.Primary.ToString()} - {bestPBInCategory.WebLink}";
							else
								return $"No PB in category {srCategory.Name} for level {srLevel.Name} was found.";
						}
						else
							return "No PBs found for this level";
					}
					else                     //Full-game run
					{
						Category srCategory;
						if (category == "")
							srCategory = srGame.FullGameCategories.ElementAt(0);
						else
							srCategory = srGame.FullGameCategories.FirstOrDefault(cat => cat.Name.ToLower().StartsWith(category.ToLower()));

						if (srCategory == null)
							return "No category was found";

						var pbs = srSearch.Users.GetPersonalBests(SpeedrunUsername, gameId: srGame.ID);

						if (pbs.Count > 0)
						{
							var SRVariables = GetVariablesForFullGameCategory(ref subcategories, ref variables, ref srCategory);

							var bestPBsInCategory = pbs.Where(x => x.Category.ID == srCategory.ID);
							if (SRVariables != null)
							{
								for (int i = 0; i < SRVariables.Count; i++)
								{
									bestPBsInCategory = bestPBsInCategory.Where(x => x.VariableValues.Contains(SRVariables[i]));
								}
							}

							if (bestPBsInCategory.Count() > 0)
								return $"Streamer\'s PB for {srGame.Name} ({srCategory.Name}) is {bestPBsInCategory.ElementAt(0).Times.Primary} - {bestPBsInCategory.ElementAt(0).WebLink}";
							else
								return "Stremer doesn\'t seem to have any PBs in category " + srCategory.Name;
						}
						else
							return "Streamer doesn\'t have any PBs in this game.";
					}
				}
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine(e.ToString());
				return "Error looking for a game on speedrun.com";
			}
		}

		private void SeperateElements(string message, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out Dictionary<string, string> lookUpSubcategories, out Dictionary<string, string> lookUpVariables)
		{
			var matchesGames = Regex.Matches(message, RegexSyntaxGame, RegexOptions.IgnoreCase);
			if (matchesGames.Count > 0)
			{
				lookUpGame = matchesGames[0].Value;
				lookUpGame = lookUpGame.Remove(0, "game:".Length);
				lookUpGame = lookUpGame.Trim(new char[] { ' ', '\"' });
			}
			else
				lookUpGame = "";

			var matchesCategories = Regex.Matches(message, RegexSyntaxCategory, RegexOptions.IgnoreCase);
			if (matchesCategories.Count > 0)
			{
				lookUpCategory = matchesCategories[0].Value;
				lookUpCategory = lookUpCategory.Remove(0, "category:".Length);
				lookUpCategory = lookUpCategory.Trim(new char[] { ' ', '\"' });
			}
			else
				lookUpCategory = "";

			var matchesLevels = Regex.Matches(message, RegexSyntaxLevel, RegexOptions.IgnoreCase);
			if (matchesLevels.Count > 0)
			{
				lookUpLevel = matchesLevels[0].Value;
				lookUpLevel = lookUpLevel.Remove(0, "level:".Length);
				lookUpLevel = lookUpLevel.Trim(new char[] { ' ', '\"' });
			}
			else
				lookUpLevel = "";

			lookUpSubcategories = null;
			lookUpVariables = null;
			;

			/*
            var matchesVariables = Regex.Matches(message, RegexVariables, RegexOptions.IgnoreCase);
            if (matchesVariables.Count > 0)
            {
                lookUpVariables = new string[matchesVariables.Count];
                for (int i = 0; i < matchesVariables.Count; i++)
                {
                    lookUpVariables[i] = matchesVariables[i].Value;
                }
            }
            else
                lookUpVariables = new string[0];*/
		}

		#region AdditionalFunctionsHandlingSRVariables
		private void SetParameters(ref bool isCurrent, ref string category, ref string level, ref Dictionary<string, string> subcategories, ref Dictionary<string, string> variables)
		{
			if (level == "" && isCurrent && LevelOverride != "")
				level = LevelOverride;

			if (category == "" && isCurrent)
			{
				if (PreferredCategory != "")
					category = PreferredCategory;
				if (CategoryOverride != "")
					category = CategoryOverride;
			}

			if (subcategories == null && isCurrent)
				subcategories = SubcategoriesOverride;

			if (variables == null && isCurrent)
				variables = VariablesOverride;
		}


		private List<VariableValue> GetVariablesForLevel(ref Dictionary<string, string> subcategories, ref Dictionary<string, string> variables, ref Category srCategory, ref Level srLevel)
		{
			List<VariableValue> lookVariable = null;
			if (subcategories != null || variables != null)
			{
				lookVariable = new List<VariableValue>();

				//Careful with the names!
				foreach (var srVariable in srCategory.Variables)
				{
					if ((srVariable.Scope.Type == VariableScopeType.AllLevels || srVariable.Scope.Type == VariableScopeType.SingleLevel || srVariable.Scope.Type == VariableScopeType.Global)
						&& (srVariable.Category == null || srVariable.CategoryID == srCategory.ID)
						&& (srVariable.Level == null || srVariable.Level == srLevel))
					{
						if (srVariable.IsSubcategory)
						{
							if (subcategories != null)
							{
								foreach (var subcategory in subcategories)
								{
									if (subcategory.Value == "")
									{
										if (srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Key) != null)
										{
											lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Key));
											break;
										}
									}
									else
									{
										if (srVariable.Name == subcategory.Key && srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Value) != null)
										{
											lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Value));
											break;
										}
									}
								}
							}
						}
						else if (variables != null)
						{
							foreach (var variable in variables)
							{
								if (variable.Value == "")
								{
									if (srVariable.Values.FirstOrDefault(x => x.Value == variable.Key) != null)
									{
										lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value == variable.Key));
										break;
									}
								}
								else
								{
									if (srVariable.Name == variable.Key && srVariable.Values.FirstOrDefault(x => x.Value == variable.Value) != null)
									{
										lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value == variable.Value));
										break;
									}
								}
							}
						}
					}
				}

				if (lookVariable.Count == 0)
					lookVariable = null;

				return lookVariable;
			}
			else
				return null;
		}
		private List<VariableValue> GetVariablesForFullGameCategory(ref Dictionary<string, string> subcategories, ref Dictionary<string, string> variables, ref Category srCategory)
		{
			if (subcategories != null || variables != null)
			{
				List<VariableValue> lookVariable = new List<VariableValue>();

				//Careful with the names!
				foreach (var srVariable in srCategory.Variables)
				{
					//Make sure we only look up the variables for selected category (it might be a case that SR returns only the ones for currently looked up category, but... eh?)
					if ((srVariable.Scope.Type == VariableScopeType.FullGame || srVariable.Scope.Type == VariableScopeType.Global)
						&& (srVariable.Category == null || srVariable.CategoryID == srCategory.ID))
					{
						if (srVariable.IsSubcategory)
						{
							if (subcategories != null)
							{
								foreach (var subcategory in subcategories)
								{
									if (subcategory.Value == "")
									{
										if (srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Key) != null)
										{
											//How deep can you nest.... deep!
											lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Key));
											break;
										}
									}
									else
									{
										if (srVariable.Name == subcategory.Key && srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Value) != null)
										{
											lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value.ToLower() == subcategory.Value));
											break;
										}
									}
								}
							}
						}
						else if (variables != null)
						{
							foreach (var variable in variables)
							{
								if (variable.Value == "")
								{
									if (srVariable.Values.FirstOrDefault(x => x.Value == variable.Key) != null)
									{
										lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value == variable.Key));
										break;
									}
								}
								else
								{
									if (srVariable.Name == variable.Key && srVariable.Values.FirstOrDefault(x => x.Value == variable.Value) != null)
									{
										lookVariable.Add(srVariable.Values.FirstOrDefault(x => x.Value == variable.Value));
										break;
									}
								}
							}
						}
					}
				}

				if (lookVariable.Count == 0)
					lookVariable = null;

				return lookVariable;
			}
			else
				return null;
		}
		#endregion
	}
}

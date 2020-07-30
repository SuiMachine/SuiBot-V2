using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SuiBot_Core.Extensions.SuiStringExtension;
using SpeedrunComSharp;

namespace SuiBot_Core.Components
{
    internal class Leaderboards
    {
        public SuiBot_ChannelInstance channelInstance;
        const string RegexSyntaxGame = "game(:|=)\".+?\"";
        const string RegexSyntaxCategory = "category(:|=)\".+?\"";
        const string RegexSyntaxLevel = "level(:|=)\".+?\"";
        const string RegexVariables = "TOBEIMPLEMENTEDORNOT";

        public bool GameOverride { get; set; }
        public string CurrentGame { get; set; }
        public string LevelOverride { get; set; }
        public string CategoryOverride { get; set; }
        public string PreferedCategory { get; set; }
        private string Speedrunusername => channelInstance.ConfigInstance.LeaderboardsUsername;
        public bool LastUpdateSuccessful { get; private set; }


        #region ProxyNamesDeclaration
        static readonly Dictionary<string, string> PROXYNAMES = new Dictionary<string, string>()
        {
            {"Star Wars: Jedi Knight - Jedi Academy", "jka" },
            {"Star Wars: Jedi Knight II - Jedi Outcast", "jk2" },
            {"Darksiders II", "Darksiders 2" },
            {"GTA3", "gtaiii" },
            {"GTA 3", "gtaiii" },
            {"Zork", "Zork I: The Great Underground Empire" },
            {"Zork I", "Zork I: The Great Underground Empire" },
            {"Zork 2", "Zork II: The Wizard of Frobozz" },
            {"Zork II", "Zork II: The Wizard of Frobozz" },
            {"Zork 3", "Zork III: The Dungeon Master" },
            {"Zork III", "Zork III: The Dungeon Master" },
            {"Thief", "Thief: The Dark Project" },
            {"Thief: Gold", "Thief Gold" },
            {"Thief 2", "Thief II: The Metal Age" },
            {"Thief II", "Thief II: The Metal Age" },
            {"F.E.A.R.: First Encounter Assault Recon", "F.E.A.R." },
            {"Judge Dredd: Dredd vs Death", "dreddgasm" },
            {"Heroes of Might and Magic II: The Price of Loyalty", "Heroes of Might and Magic II" },
            {"Heroes of Might and Magic III: The Restoration of Erathia", "Heroes of Might and Magic III" },
            {"Heroes of Might and Magic III: Armageddon's Blade", "Heroes of Might and Magic III" },
            {"Heroes of Might and Magic III: In the wake of gods", "Heroes of Might and Magic III" },
            {"Heroes of Might and Magic III: The Shadow of Death", "Heroes of Might and Magic III" },
            {"Heroes of Might and Magic IV", "Heroes of Might and Magic III" },
            {"Heroes of Might and Magic 3", "Heroes of Might and Magic III" },
            {"homm3", "Heroes of Might and Magic III" },
            {"Trespasser: Jurassic Park", "Jurassic Park: Trespasser" },
            {"Hitman (2016)", "just_hitman" },
            {"Star Wars: Knights of the Old Republic II - The Sith Lords", "Star Wars: Knights of the Old Republic 2 - The Sith Lords" },
            {"Quake II", "Quake II (PC)" },
            {"DMC", "DmC: Devil May Cry" },
            {"Hexen 2", "Hexen II" },
            {"Dark Souls 2", "Dark Souls II" },
            {"Dark Souls 3", "Dark Souls III" },
            {"Command & Conquer Remastered Collection", "cncremastered" }
        };

        private static string GetProxyName(string lookUpGame)
        {
            lookUpGame = lookUpGame.ToLower().Trim();
            foreach(var element in PROXYNAMES)
            {
                if (element.Key.ToLower() == lookUpGame)
                    return element.Value;
            }
            return lookUpGame;
        }
        #endregion

        public Leaderboards(SuiBot_ChannelInstance channelInstance)
        {
            this.channelInstance = channelInstance;
            GameOverride = false;
            CurrentGame = "";
            LevelOverride = "";
            CategoryOverride = "";
            PreferedCategory = "";
        }

        public void SetPreferedCategory(string StreamTitle, bool vocal = false)
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
                            if (PreferedCategory != category.Name || vocal)
                            {
                                PreferedCategory = category.Name;
                                LastUpdateSuccessful = true;
                                channelInstance.SendChatMessage(string.Format("Set leaderboards category to: \"{0}\" based on stream title", PreferedCategory));
                            }
                            return;
                        }
                    }
                    PreferedCategory = "";
                    LastUpdateSuccessful = true;
                    channelInstance.SendChatMessage("Haven't found the category in stream title.");
                    return;
                }
                LastUpdateSuccessful = true;
                if(vocal)
                    channelInstance.SendChatMessage("Haven't found the game on speedrun.com. !leaderboards game %GAME TITLE% might be used to force the game.");
            }
            catch (Exception e)
            {
                ErrorLogging.WriteLine("Error setting prefered category: " + e.Message);
                LastUpdateSuccessful = false;
            }
        }

        public void DoModWork(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();

            if(lastMessage.Message.StartsWithLazy("username"))
            {
                lastMessage.Message = lastMessage.Message.StripSingleWord();
                if(lastMessage.Message != "")
                {
                    channelInstance.ConfigInstance.LeaderboardsUsername = lastMessage.Message;
                    channelInstance.ConfigInstance.Save();
                    channelInstance.SendChatMessageResponse(lastMessage, "Set Speedrun username to: " + Speedrunusername);
                }
                else
                {
                    channelInstance.ConfigInstance.LeaderboardsUsername = lastMessage.Message;
                    channelInstance.ConfigInstance.Save();
                    channelInstance.SendChatMessageResponse(lastMessage, "Nulled out speedrun username");
                }
            }
            else if(lastMessage.Message.StartsWithLazy("game"))
            {
                lastMessage.Message = lastMessage.Message.StripSingleWord();
                if (lastMessage.Message != "")
                {
                    GameOverride = true;
                    CurrentGame = lastMessage.Message;
                    channelInstance.SendChatMessageResponse(lastMessage, "Set game override to: " + CurrentGame);
                }
                else
                {
                    GameOverride = false;
                    channelInstance.SendChatMessageResponse(lastMessage, "Disabled game override (game will be updated on next twitch status update).");
                }
            }
            else if(lastMessage.Message.StartsWithLazy("level"))
            {
                lastMessage.Message = lastMessage.Message.StripSingleWord();
                if (lastMessage.Message != "")
                {
                    LevelOverride = lastMessage.Message;
                    channelInstance.SendChatMessageResponse(lastMessage, "Set level to: " + LevelOverride);
                }
                else
                {
                    LevelOverride = "";
                    channelInstance.SendChatMessageResponse(lastMessage, "Disabled level override.");
                }
            }
            else if (lastMessage.Message.StartsWithLazy("category"))
            {
                lastMessage.Message = lastMessage.Message.StripSingleWord();
                if (lastMessage.Message != "")
                {
                    CategoryOverride = lastMessage.Message;
                    channelInstance.SendChatMessageResponse(lastMessage, "Set category to: " + CategoryOverride);
                }
                else
                {
                    CategoryOverride = "";
                    channelInstance.SendChatMessageResponse(lastMessage, "Disabled category override.");
                }
            }
        }

        public void DoWorkWR(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();

            if (lastMessage.Message == "")
            {
                channelInstance.SendChatMessageResponse(lastMessage, GetWR(CurrentGame, true, "", "", new string[0]));
            }
            else
            {
                bool isCurrentGame = false;
                SeperateElements(lastMessage.Message, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out string[] lookUpVariables);

                //Sort out game name
                if (lookUpGame.ToLower() == "this")
                {
                    lookUpGame = CurrentGame;
                    isCurrentGame = true;
                }

                if (lookUpGame == "")
                {
                    if (lookUpCategory == "" && lookUpLevel == "" && lookUpLevel == "" && lookUpVariables.Length == 0)
                    {
                        lookUpGame = lastMessage.Message;
                    }
                    else
                    {
                        lookUpGame = CurrentGame;
                        isCurrentGame = true;
                    }
                }
                lookUpGame = GetProxyName(lookUpGame);

                if (lookUpGame == "")
                {
                    channelInstance.SendChatMessageResponse(lastMessage, "No game name provided");
                }
                else
                {
                    channelInstance.SendChatMessageResponse(lastMessage, GetWR(lookUpGame, isCurrentGame, lookUpCategory, lookUpLevel, lookUpVariables));
                }
            }
        }

        private string GetWR(string game, bool isCurrent, string category, string level, string[] variables)
        {
            try
            {
                if (level == "" && isCurrent && LevelOverride != "")
                    level = LevelOverride;

                if (category == "" && isCurrent)
                {
                    if (PreferedCategory != "")
                        category = PreferedCategory;
                    if (CategoryOverride != "")
                        category = CategoryOverride;
                }

                if (game == "")
                    return "No game provided";

                var srSearch = new SpeedrunComClient();
                var srGame = srSearch.Games.SearchGame(game);
                if (srGame == null)
                    return "No game was found";
                else
                {
                    //Levels generally have different category, so they need a seperate look up
                    if(level != "")
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

                        var leaderboard = srSearch.Leaderboards.GetLeaderboardForLevel(srGame.ID, srLevel.ID, srCategory.ID, variableFilters: null);

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
                            return string.Format("No records were found! - {0}", leaderboard.WebLink);
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

                        var leaderboard = srSearch.Leaderboards.GetLeaderboardForFullGameCategory(srGame.ID, srCategory.ID, variableFilters: null);

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

                            return string.Format("{0} ({1}) record is {2} by {3} - {4}",
                                srGame.Name,
                                srCategory.Name,
                                record.Times.Primary.ToString(),
                                runners,
                                record.WebLink
                                );
                        }
                        else
                            return "Leaderboard doesn't have any records! " + leaderboard.WebLink; ;
                    }
                }
            }
            catch(Exception e)
            {
                ErrorLogging.WriteLine(e.ToString());
                return "Error looking for a game on speedrun.com.";
            }
        }

        internal void DoWorkPB(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();

            if (lastMessage.Message == "")
            {
                channelInstance.SendChatMessageResponse(lastMessage, GetPB(CurrentGame, true, "", "", new string[0]));
            }
            else
            {
                bool isCurrentGame = false;
                SeperateElements(lastMessage.Message, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out string[] lookUpVariables);

                //Sort out game name
                if (lookUpGame.ToLower() == "this")
                {
                    lookUpGame = CurrentGame;
                    isCurrentGame = true;
                }

                if (lookUpGame == "")
                {
                    if (lookUpCategory == "" && lookUpLevel == "" && lookUpLevel == "" && lookUpVariables.Length == 0)
                    {
                        lookUpGame = lastMessage.Message;
                    }
                }
                lookUpGame = GetProxyName(lookUpGame);

                if (lookUpGame == "")
                {
                    channelInstance.SendChatMessageResponse(lastMessage, "No game name provided");
                }
                else
                {
                    channelInstance.SendChatMessageResponse(lastMessage, GetPB(lookUpGame, isCurrentGame, lookUpCategory, lookUpLevel, lookUpVariables));
                }
            }
        }

        private string GetPB(string game, bool isCurrentGame, string category, string level, string[] variables)
        {
            try
            {
                var srSearch = new SpeedrunComClient();
                var srGame = srSearch.Games.SearchGame(game);

                if (level == "" && isCurrentGame && LevelOverride != "")
                    level = LevelOverride;

                if (category == "" && isCurrentGame)
                {
                    if (PreferedCategory != "")
                        category = PreferedCategory;
                    if (CategoryOverride != "")
                        category = CategoryOverride;
                }

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

                        var pbs = srSearch.Users.GetPersonalBests(Speedrunusername, gameId: srGame.ID);

                        var levelPBs = pbs.Where(x => x.LevelID == srLevel.ID);
                        if(levelPBs.Count() > 0)
                        {
                            var bestPBInCategory = levelPBs.FirstOrDefault(x => x.Category.ID == srCategory.ID);
                            if (bestPBInCategory != null)
                                return string.Format("Streamer\'s PB for {0} ({1}) in level \"{2}\" is {3} - {4}", srGame.Name, srCategory.Name, srLevel.Name, bestPBInCategory.Times.Primary.ToString(), bestPBInCategory.WebLink);
                            else
                                return string.Format("No PB in category {0} for level {1} was found.", srCategory.Name, srLevel.Name);
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

                        var pbs = srSearch.Users.GetPersonalBests(Speedrunusername, gameId: srGame.ID);

                        if (pbs.Count > 0)
                        {
                            var filteredOutPBs = pbs.Where(x => x.CategoryID == srCategory.ID);
                            if (filteredOutPBs.Count() > 0)
                            {
                                return string.Format("Streamer\'s PB for {0} ({1}) is {2} - {3}",
                                    srGame.Name,
                                    srCategory.Name,
                                    filteredOutPBs.ElementAt(0).Times.Primary.ToString(),
                                    filteredOutPBs.ElementAt(0).WebLink);
                            }
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

        private void SeperateElements(string message, out string lookUpGame, out string lookUpCategory, out string lookUpLevel, out string[] lookUpVariables)
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

            lookUpVariables = new string[0];

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
    }
}

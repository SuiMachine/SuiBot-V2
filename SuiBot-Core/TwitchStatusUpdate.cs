using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core
{
    public class TwitchStatusUpdate
    {
        string channelName;
        public bool isOnline = true;
        public string game = "";
        private string oldId = "x";
        public uint LastViewers = 0;
        Uri sUrlTwitchStatus = null;
        Dictionary<string, string> RequestHeaders;

        public TwitchStatusUpdate(string channelName)
        {
            RequestHeaders = new Dictionary<string, string>();
            sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + channelName);
            this.channelName = channelName;
            RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
        }

        public TwitchStatusUpdate(SuiBot_ChannelInstance suiBot_ChannelInstance)
        {
            RequestHeaders = new Dictionary<string, string>();
            this.channelName = suiBot_ChannelInstance.Channel;
            sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + suiBot_ChannelInstance.Channel);
            RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
        }

        public void GetStatus()
        {
            if (JsonGrabber.GrabJson(sUrlTwitchStatus, RequestHeaders, "application/json", "application/vnd.twitchtv.v3+json", "GET", out string res))
            {
                if (res.Contains("title"))
                {
                    isOnline = true;
                    int indexStart = res.IndexOf("type");
                    if (indexStart > 0)
                    {
                        indexStart += "type".Length + 3;
                        int indexEnd = res.IndexOf(",", indexStart);
                        string thatThing = res.Substring(indexStart, indexEnd - indexStart - 1).ToLower();
                        if (thatThing == "live")
                            isOnline = true;
                        else
                            isOnline = false;
                    }

                    indexStart = res.IndexOf("game_id");
                    if (indexStart > 0)
                    {
                        indexStart += "game_id".Length + 3;
                        int indexEnd = res.IndexOf(",", indexStart) - 1;
                        game = ResolveNameFromId(res.Substring(indexStart, indexEnd - indexStart));
                        if (game == "ul")
                        {
                            game = String.Empty;
                        }
                        Console.WriteLine("Stream is online, game: " + game);
                    }
                    else
                    {
                        Console.WriteLine("Checked stream status. Is online.");
                    }

                    indexStart = res.IndexOf("viewers");
                    if (indexStart > 0)
                    {
                        indexStart += "viewers".Length + 3;
                        int indexEnd = res.IndexOf(",", indexStart);
                        if (uint.TryParse(res.Substring(indexStart, indexEnd - indexStart), out uint Value))
                        {
                            this.LastViewers = Value;
                        }
                    }
                }
                else
                {
                    isOnline = false;
                    Console.WriteLine("Checked stream status. Is offline.");
                }
            }
            else
            {
                Console.WriteLine("Error checking Json");
            }
        }

        public string ResolveNameFromId(string id)
        {
            if (oldId == id)
            {
                return game;
            }
            else
            {
                if (JsonGrabber.GrabJson(new Uri("https://api.twitch.tv/helix/games?id=" + id), RequestHeaders, "application/json", "application/vnd.twitchtv.v3+json", "GET", out string res))
                {
                    oldId = id;
                    int indexStart = res.IndexOf("name");
                    if (indexStart > 0)
                    {
                        indexStart += "name".Length + 3;
                        int indexEnd = res.IndexOf(",", indexStart);
                        return res.Substring(indexStart, indexEnd - indexStart - 1);
                    }
                }
                return "";
            }

        }

        public string GetStreamTime()
        {
            string res = "";
            if (JsonGrabber.GrabJson(sUrlTwitchStatus, RequestHeaders, "application/json", "application/vnd.twitchtv.v3+json", "GET", out res))
            {
                if (res.Contains("display_name"))
                {
                    isOnline = true;
                    string temp = Convert.ToString(res);
                    int indexStart = temp.IndexOf("created_at");

                    if (indexStart > 0)
                    {
                        indexStart = indexStart + 13;
                        int indexEnd = temp.IndexOf(",", indexStart) - 2;
                        string output = temp.Substring(indexStart, indexEnd - indexStart);
                        if (DateTime.TryParse(output, out DateTime dt))
                        {
                            TimeSpan difference = DateTime.UtcNow - dt;
                            return "on " + dt.Date.ToShortDateString() + " at " + dt.TimeOfDay.ToString() + " -- " + difference.Hours.ToString("00") + ":" + difference.Minutes.ToString("00") + ":" + difference.Seconds.ToString("00");
                        }
                        else
                            return "";

                    }
                }
                else
                {
                    isOnline = false;
                }
                return "";
            }
            return "";
        }

        internal void RequestUpdate(SuiBot_ChannelInstance instance)
        {
            GetStatus();
            if (game != string.Empty)
            {
                instance.SendChatMessage("New isOnline status is - \'" + isOnline.ToString() + "\' and the game is: " + game);
            }
            else
            {
                instance.SendChatMessage("New isOnline status is - " + isOnline.ToString());
            }
        }
    }
}

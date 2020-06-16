using Newtonsoft.Json.Linq;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuiBot_Core.Components
{
    internal class PCGW
    {
        private const string PCGW_API_URI = "https://www.pcgamingwiki.com/w/api.php";

        private SuiBot_ChannelInstance ChannelInstance;
        private TwitchStatusUpdate TwitchUpdateInstance;

        public PCGW(SuiBot_ChannelInstance ChannelInstance, TwitchStatusUpdate TwitchUpdateInstance)
        {
            this.ChannelInstance = ChannelInstance;
            this.TwitchUpdateInstance = TwitchUpdateInstance;
        }

        internal void DoWork(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();

            if(lastMessage.Message == "")
            {
                if (TwitchUpdateInstance.game == "")
                {
                    ChannelInstance.SendChatMessageResponse(lastMessage, "Current game is empty. You can try providing the game manually by using command \"!pcgw Game Name\"");
                    return;
                }
                else
                {
                    ChannelInstance.SendChatMessageResponse(lastMessage, GetPCGWUrl(TwitchUpdateInstance.game));
                    return;
                }

            }
            else
            {
                ChannelInstance.SendChatMessageResponse(lastMessage, GetPCGWUrl(lastMessage.Message));
                return;
            }
        }

        internal string GetPCGWUrl(string GameName)
        {
            try
            {
                if (JsonGrabber.GrabJson(JsonGrabber.BuildRequestUri(PCGW_API_URI, new string[] {
                    JsonGrabber.FormatParameter("action", "ask"),
                    JsonGrabber.FormatParameter("format", "json"),
                    JsonGrabber.FormatParameter("query", "[[Category:Games]] [[" + GameName + "]]")
                }), out string result))
                {
                    if (result != "")
                    {
                        var response = JObject.Parse(result);
                        if (response["query"] != null)
                        {
                            var queryData = response["query"];
                            if (queryData["results"] != null)
                            {
                                var resultData = queryData["results"];
                                if (resultData.Count() > 0)
                                {
                                    return resultData.First.First["fullurl"].ToString();
                                }
                                else
                                    return "Ask query returned no results. You may want to try: " + string.Format("https://www.pcgamingwiki.com/w/index.php?search={0}", Uri.EscapeDataString(GameName));
                            }
                            else
                                return "Query returned no result node! Something is wrong...";
                        }
                        else
                            return "Query returned with no \"Query\" node in response!";
                    }
                    else
                        return "Response was empty!";
                }
                else
                    return "Failed to get response from PCGW.";

            }
            catch(Exception e)
            {
                ErrorLogging.WriteLine("[ERROR WITH PCGW REQUEST] - " + e.Message);
                return "Exception thrown when trying to query PCGW. See log file.";
            }
        }
    }
}

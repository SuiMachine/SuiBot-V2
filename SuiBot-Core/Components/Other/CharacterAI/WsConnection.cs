using CharacterAi.Client.Models.WS;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class WsConnection
{
    public WebSocket4Net.WebSocket Client { get; set; }

    /// <summary>
    /// request_id : message
    /// </summary>
    public List<WsResponseMessage> Messages { get; } = new List<WsResponseMessage>();

    public void Send(string message)
        => Client.Send(message);

    public string GetAllMessagesFormatted() => string.Join("; ", Messages.Select(JsonConvert.SerializeObject));

}
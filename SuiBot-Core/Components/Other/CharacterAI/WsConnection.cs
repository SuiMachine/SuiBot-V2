using CharacterAi.Client.Models.WS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class WsConnection
{
	public WebSocket Client { get; set; }
	public CancellationToken CancellationToken { get; set; }

	/// <summary>
	/// request_id : message
	/// </summary>
	public List<WsResponseMessage> Messages { get; } = new List<WsResponseMessage>();

	public void SendAsync(string message)
	{
		if (Client.State == WebSocketState.Open)
		{
			var data = System.Text.Encoding.UTF8.GetBytes(message);
			var cancelationToken = new CancellationToken();
			Task.Run(async () =>
			{
				await Client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancelationToken);
			});
		}
	}

	public string GetAllMessagesFormatted() => string.Join("; ", Messages.Select(JsonConvert.SerializeObject));

}
using SpeedrunComSharp;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBotAI.Components.Other.Gemini;
using System;
using System.Linq;
using System.Text;

namespace SuiBot_Core.Components.Other.Gemini.Speedrun
{

	[Serializable]
	public class SpeedrunWRCall : SuiBotFunctionCall
	{
		[FunctionCallParameter(true)]
		public string game_name = null;
		[FunctionCallParameter(false)]
		public string category = null;

		public override string FunctionDescription() => "Gets best time (world record) speedrunning leaderboard if it exists";

		public override string FunctionName() => "speedrun_world_record";

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
		{
			if (game_name == null)
				return;
			var speedrunClient = new SpeedrunComClient();
			var game = speedrunClient.Games.SearchGame(game_name);
			if (game == null)
				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "No game was found."));
			else
			{
				var categories = game.FullGameCategories;

				var foundCategory = string.IsNullOrEmpty(category) ? null : categories.FirstOrDefault(x => x.Name == category);

				if (string.IsNullOrEmpty(category) || foundCategory == null)
				{
					StringBuilder sb = new StringBuilder();

					if (!string.IsNullOrEmpty(category))
					{
						sb.AppendLine("Incorrect category name");
						sb.AppendLine("");
					}

					sb.AppendLine("Available categories are:");
					foreach (var category in categories)
					{
						sb.AppendLine($"* {category.Name}");
					}
					sb.AppendLine($"The default category is: {game.FullGameCategories.ElementAt(0)}");
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), sb.ToString()));
				}
				else
				{
					StringBuilder sb = new StringBuilder();
					if (foundCategory.WorldRecord != null)
						sb.AppendLine($"Best time for {foundCategory.Name} is {foundCategory.WorldRecord.Times.Primary.Value} by {foundCategory.WorldRecord.Player.Name}.");
					else
						sb.AppendLine($"Category {foundCategory.Name} doesn't have any records.");

					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), sb.ToString()));
				}
			}
		}
	}

	[Serializable]
	public class SpeedrunPBCall : SuiBotFunctionCall
	{
		[FunctionCallParameter(true)]
		public string username = null;
		[FunctionCallParameter(true)]
		public string game_name = null;
		[FunctionCallParameter(false)]
		public string category = null;

		public override string FunctionDescription() => "Gets user's personal best from speedrunning leaderboard if it exists";

		public override string FunctionName() => "speedrun_personal_best";

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, SuiBotAI.Components.Other.Gemini.GeminiContent content)
		{
			if (game_name == null)
				return;
			var speedrunClient = new SpeedrunComClient();
			var game = speedrunClient.Games.SearchGame(game_name);
			if (game == null)
				channelInstance?.GeminiAI?.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "No game was found."));
			else
			{
				var categories = game.FullGameCategories;

				var foundCategory = string.IsNullOrEmpty(category) ? null : categories.FirstOrDefault(x => x.Name == category);

				if (category == null || foundCategory == null)
				{
					StringBuilder sb = new StringBuilder();
					if (category != null)
					{
						sb.AppendLine("Invalid category name!");
						sb.AppendLine("");
					}

					sb.AppendLine("Available categories are:");
					foreach (var category in categories)
					{
						sb.AppendLine($"* {category.Name}");
					}
					sb.AppendLine($"The default category is: {game.FullGameCategories.ElementAt(0)}");
					channelInstance?.GeminiAI?.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), sb.ToString()));
				}
				else
				{
					var pbs = speedrunClient.Users.GetPersonalBests(username, gameId: game.ID);
					if (pbs.Count == 0)
					{
						string response = $"{username} has not done any runs in this game.";
						channelInstance?.GeminiAI?.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), response));
						return;
					}

					var matchingPB = pbs.FirstOrDefault(x => x.CategoryID == foundCategory.ID);
					if (matchingPB == null)
					{
						string response = $"{username} has not done any runs in category {foundCategory.Name}.";
						channelInstance?.GeminiAI?.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), response));
					}
					else
					{
						string response = $"{username} best time in {foundCategory.Name} is {matchingPB.Times.Primary.Value}.";

						channelInstance?.GeminiAI?.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), response));
					}

				}
			}
		}
	}
}

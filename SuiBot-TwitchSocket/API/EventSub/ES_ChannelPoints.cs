using System;

namespace SuiBot_Core.API.EventSub
{
	public class ES_ChannelPoints
	{
		public enum RedemptionStates
		{
			UNFULFILLED,
			FULFILLED,
			CANCELED
		}

		public class ChannelPointRedeemRequest
		{
			public string userName;
			public string userId;
			public string rewardId;
			public string redemptionId;
			public RedemptionStates state;
			public string userInput;

			public ChannelPointRedeemRequest(string userName, string userId, string rewardId, string redemptionId, RedemptionStates state, string userInput)
			{
				this.userName = userName;
				this.userId = userId;
				this.rewardId = rewardId;
				this.redemptionId = redemptionId;
				this.state = state;
				this.userInput = userInput;
			}
		}

		[Serializable]
		public class ChannelReward
		{
			[Serializable]
			public class Global_Cooldown_Setting
			{
				public bool is_enabled;
				public int global_cooldown_seconds;
			}

			public string id = "";
			public bool is_enabled = false;
			public int cost = 0;
			public string title = "";
			public string prompt = "";
			public bool is_user_input_required = false;
			public bool is_paused = false;
			public bool should_redemptions_skip_request_queue = false;
			public Global_Cooldown_Setting global_cooldown_setting;

			public static bool Differs(ChannelReward l, ChannelReward r)
			{
				return l.id != r.id
					|| l.is_enabled != r.is_enabled
					|| l.cost != r.cost
					|| l.title != r.title
					|| l.prompt != r.prompt
					|| l.is_user_input_required != r.is_user_input_required
					|| l.is_paused != r.is_paused
					|| l.should_redemptions_skip_request_queue != r.should_redemptions_skip_request_queue
					|| l.global_cooldown_setting.is_enabled != r.global_cooldown_setting.is_enabled
					|| l.global_cooldown_setting.global_cooldown_seconds != r.global_cooldown_setting.global_cooldown_seconds;
			}
		}

		//Because Twitch API is a bit of a mess
		[Serializable]
		public class ChannelRewardRequest
		{
			public string id = "";
			public bool is_enabled = false;
			public int cost = 0;
			public string title = "";
			public string prompt = "";
			public bool is_user_input_required = false;
			public bool is_paused = false;
			public bool should_redemptions_skip_request_queue = false;
			public bool is_global_cooldown_enabled = false;
			public int global_cooldown_seconds = 0;

			public static bool Differs(ChannelRewardRequest l, ChannelReward r)
			{
				return l.id != r.id
					|| l.is_enabled != r.is_enabled
					|| l.cost != r.cost
					|| l.title != r.title
					|| l.prompt != r.prompt
					|| l.is_user_input_required != r.is_user_input_required
					|| l.is_paused != r.is_paused
					|| l.should_redemptions_skip_request_queue != r.should_redemptions_skip_request_queue
					|| l.is_global_cooldown_enabled != r.global_cooldown_setting.is_enabled
					|| l.global_cooldown_seconds != r.global_cooldown_setting.global_cooldown_seconds;
			}
		}
	}
}

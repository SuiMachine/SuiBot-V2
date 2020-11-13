namespace SuiBot_Core.Components
{
	class ViewerPB
	{
		private Storage.ViewerPBStorage pbStorage { get; set; }
		SuiBot_ChannelInstance ChannelInstance { get; set; }

		public ViewerPB(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
			this.pbStorage = Storage.ViewerPBStorage.Load(ChannelInstance.Channel);
		}

		public void DoWork(ChatMessage lastMessage)
		{
			ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Current recorded ViewerPB for this channel is {0} viewers.", pbStorage.ViewerPB));
		}

		public void UpdateViewerPB(uint LastCheckedViewerAmount)
		{
			if (LastCheckedViewerAmount > pbStorage.ViewerPB)
			{
				pbStorage.ViewerPB = LastCheckedViewerAmount;
				pbStorage.Save();
			}
		}
	}
}

namespace SuiBot_Core.API.Helix
{
	public struct API_Data
	{
		public object data;
	}

	public struct API_Timeout
	{
		public string user_id;
		public uint duration;
		public string reason;
	}
}

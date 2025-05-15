namespace SuiBot_Core.API.Helix.Responses
{
	public class Response_ValidateToken
	{
		public string client_id;
		public string login;
		public string[] scopes;
		public ulong user_id;
		public ulong expires_in;
	}
}

namespace LiteInvest.Entity.ServerEntity
{
	public class LoginInfo
	{
		public string Name { get; set; }
		//private bool Admin { get; set; }
		public string token { get; set; }
		public DateTime expirationTime { get; set; }

		public string errorMessage { get; set; }

	}
}

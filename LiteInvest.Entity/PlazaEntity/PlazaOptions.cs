namespace LiteInvest.Entity.PlazaEntity
{
	public class PlazaOptions
	{
		public bool Simulation { get; set; }
        public PlazaOptions()
        {
            Simulation = true;
        }

        public static readonly List<int> ScaleList = new List<int>() { 1, 5, 10, 15, 20, 25, 30,  50, 100 };
    }
}

namespace LiteInvestServerDll.Options
{
	public class PlazaOptions
	{
		public bool Simulation { get; set; }

		public PlazaOptions(bool isSimulation)
		{
			Simulation = isSimulation;
		}
	}
}

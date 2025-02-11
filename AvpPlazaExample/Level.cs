namespace AvpPlazaExample
{
    public class Level
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }

        public int TypeLevel { get; set; }
        public Level(decimal price, decimal vol, int typeLevel)
        {
            Price = price;
            Volume = vol;
            TypeLevel = typeLevel;
        }
    }
}

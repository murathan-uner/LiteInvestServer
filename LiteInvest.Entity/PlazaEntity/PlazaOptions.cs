using LiteInvest.Entity.ServerEntity;

namespace LiteInvest.Entity.PlazaEntity
{
	public class PlazaOptions
	{
		public bool Simulation { get; set; }

        public DBSaver DBSaver { get; set; }

        public Dictionary<BoardEnum, TimeSpan> TimeClearingSetting { get; set; }

        public decimal GetMoneyMin{ get; set; }

        /// <summary>
        /// Automatic scrolling ON/OFF flag
        /// </summary>
        public bool AutomaticScrolling { get; set; }

        /// <summary>
        /// Like trades volumes list: [1,2,3,4,5]
        /// </summary>
        public List<int> LikeTradesVolume { get; set; } = [];

        private static PlazaOptions? Instance;

        /// <summary>
        /// Формула расчета штрафа за превышение объема перенесенных позиций (акции и фьючерсы) через основной
        /// клиринг. Для трейдеров у кого депозит более 10 000 рублей перенос допустим не более, чем на 5 плечо и не более, чем на 5
        /// млн сверх депозита.При наступлении любого из двух условий списывается штраф по ставке РЕПО.
        /// </summary>
        public decimal REPOSetting {  get; set; }

        /// <summary>
        /// Штраф за превышение просадки составляет 20% от суммы превышения просадки. 
        /// </summary>
        public decimal PenaltyOverDrawDownSetting { get; set; }

        /// <summary>
        /// Create in program.cs in builder.Configuration.GetSection("PlazaOptions").Get<PlazaOptions>();
        /// and remember object in static Instance 
        /// </summary>
        public PlazaOptions()
        {
            PlazaOptions.Instance = this;
        }

        /// <summary>
        /// Список масштабов агрегации по стаканам и кластерам
        /// </summary>
        public static readonly List<int> ScaleList = new List<int>() { 1, 5, 10, 15, 20/*, 25, 30,  50, 100*/};

        /// <summary>
        /// Размер чистого агрегированного стакана и кластера в каждую сторону. Полный размер х2.
        /// Штук шагов цены PriceStep
        /// </summary>
        public static readonly int SizeBlankMarketLevelsCountPriceStep = 1500;

        public static Dictionary<BoardEnum, TimeSpan> TimeClearing
        {
            get => Instance?.TimeClearingSetting;
        }

        /// <summary>
        /// Тайм фрейм для построения кластера
        /// </summary>
        public static TimeSpan TimeFrameClusters = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Установленное значение точки сохранения параметров 
        /// </summary>
        /// <returns></returns>
        public static DBSaver? MyDBSaver
        {
            get => Instance?.DBSaver;
        }

        /// <summary>
        /// Установленное значение точки сохранения параметров 
        /// </summary>
        /// <returns></returns>
        public static decimal MyGetMoneyMin
        {
            get => (decimal)Instance?.GetMoneyMin;
        }

        /// <summary>
        /// Установленное значение точки сохранения параметров 
        /// </summary>
        /// <returns></returns>
        public static bool Simulations
        {
            get => (bool)Instance?.Simulation;
        }

        /// <summary>
        /// РЕПО штрафа за превышение объема перенесенных позиций (акции и фьючерсы) через основной
        /// клиринг. Для трейдеров у кого депозит более 10 000 рублей перенос допустим не более, чем на 5 плечо и не более, чем на 5
        /// млн сверх депозита.При наступлении любого из двух условий списывается штраф по ставке РЕПО.
        /// </summary>
        public static decimal REPO
        {
            get => (decimal)Instance?.REPOSetting;
        }

        /// <summary>
        /// Штраф за превышение просадки составляет 20% от суммы превышения просадки. 
        /// </summary>
        public static decimal PenaltyOverDrawDown
        {
            get => (decimal)Instance?.PenaltyOverDrawDownSetting;
        }

    }




    /// <summary>
    /// Варианты реализации сохранения настроек, 
    /// ордеров позиций, пользователей и прочих параметров
    /// </summary>
    public enum DBSaver
    {
        XML,
        DB
    }

}

namespace LiteInvestFront.Components.Common
{
    public class Constants
    {
        public const decimal DEFAULT_WINDOWS_HEIGHT = 900; // pixel
        public const decimal DEFAULT_CHART_FULL_WIDTH = 2000; // pixel

        public const int DEFAULT_CHART_AMOUNT = 100;
        //public const int DEFAULT_CHART_AMOUNT = 30;

        public const decimal DEFAULT_CHART_Y_MIN = -200; // relative value to windows height = 900 pixels
        public const decimal DEFAULT_CHART_Y_MAX = 300; // relative value to windows height = 900 pixels
        public const decimal DEFAULT_CHART_MAX_VOLUME = 3000; // relative value to windows height = 900 pixels

        public const decimal DEFAULT_CHART_Y_RANGE = DEFAULT_CHART_Y_MAX - DEFAULT_CHART_Y_MIN;
        public const decimal DEFAULT_CHART_Y_MIN_RATIO = DEFAULT_CHART_Y_MIN / DEFAULT_WINDOWS_HEIGHT;
        public const decimal DEFAULT_CHART_Y_MAX_RATIO = DEFAULT_CHART_Y_MAX / DEFAULT_WINDOWS_HEIGHT;
        public const decimal DEFAULT_CHART_MAX_VOLUME_RATIO = DEFAULT_CHART_MAX_VOLUME / DEFAULT_WINDOWS_HEIGHT;

        public const string VIEWPORT_HEIGHT_SIZE = "calc(100vh - 100px)";
    }
}

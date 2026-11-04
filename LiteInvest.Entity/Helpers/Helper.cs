using System.Runtime.Serialization;
using System.Web;
using System.Xml;

namespace LiteInvest.Entity.Helpers
{


	public static class UriExtensions
	{
		/// <summary>
		/// Adds the specified parameter to the Query String.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="paramName">Name of the parameter to add.</param>
		/// <param name="paramValue">Value for the parameter to add.</param>
		/// <returns>Url with added parameter.</returns>
		public static Uri AddParameter(this Uri url, string paramName, string paramValue)
		{
			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query[paramName] = paramValue;
			uriBuilder.Query = query.ToString();

			return uriBuilder.Uri;
		}
	}

	public class Helper
    {
        private static object saveXmlLocker = new();
        public static void SaveXml<T>(T serializableObject, string name)
        {
            lock (saveXmlLocker)
            {
                var settings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t",

                };
                using (var writer = XmlWriter.Create(name, settings))
                {
                    new DataContractSerializer(typeof(T)).WriteObject(writer, serializableObject);
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// Чтение из XML
        /// Если такого файла нет, то создает пустой 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T? ReadXml<T>(string name)
        {
            try
            {
                var fileStream = new FileStream(name, FileMode.Open,FileAccess.Read);
                var reader = XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas());
                var serializer = new DataContractSerializer(typeof(T));
                T serializableObject = (T)serializer.ReadObject(reader, true);
                reader.Close();
                fileStream.Close();
                return serializableObject;
            }
            catch (Exception ex)
            {
                //TODO: Крайне жесткое место, если вылетает ошибка, то это пизда.
                //Диспоуза нет и потом запись в это же место не работает лол. 
                Console.WriteLine($"Fatal error reading {name}");
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        public static System.Timers.Timer CreateTimerAndStart(Action method, int ms, bool repeat = true)
        {
            var timer = new System.Timers.Timer(ms) { AutoReset = repeat };
            timer.Elapsed += (s, e) =>
            {
                method?.Invoke();
            };
            timer.Start();
            return timer;
        }

        public static System.Timers.Timer CreateTimerAndStartAsync(Func<Task> method, int ms, bool repeat = true)
        {
            var timer = new System.Timers.Timer(ms) { AutoReset = repeat };
            timer.Elapsed += async (s, e) =>
            {
                if (method != null)
                {
                    await method();
                }
            };
            timer.Start();
            return timer;
        }

        public static bool isSimulation()
        {
            
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                return true;

            return false;
            //return true;
        }

        public static DateTime DateTimeMSK
        {
            get => DateTime.UtcNow.AddHours(3);
        }

        public static long GetSafeUnixTimestamp(DateTime datetime)
        {
            // Check for minimum DateTime value (0001-01-01)
            if (datetime == DateTime.MinValue || datetime.Year < 1970) // Unix epoch starts 1970
            {
                return 0;
            }

            try
            {
                return new DateTimeOffset(datetime).ToUnixTimeMilliseconds();
            }
            catch (ArgumentOutOfRangeException)
            {
                // Handle dates that are too early or too late for Unix timestamp
                return 0;
            }
            catch (Exception ex)
            {
                // Log other unexpected errors
                Console.WriteLine($"Error converting DateTime to Unix timestamp: {ex.Message}");
                return 0;
            }
        }
    }


    public static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> source) where T : ICloneable
             => source.Select(item => (T)item.Clone()).ToList();

        public static IList<T> Clone<T>(this ICollection<T> source) where T : ICloneable
            => source.Select(item => (T)item.Clone()).ToList();

        public static IList<T> Clone<T>(this IEnumerable<T> source) where T : ICloneable
          => source.Select(item => (T)item.Clone()).ToList();
    }

}

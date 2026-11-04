using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LiteInvestServerDll
{
    public class ReportSender
    {
       
    
        // Токен вашего Telegram-бота (получить у @BotFather)
        private static readonly string BotToken = "6900125343:AAFo0qdYGrzY_3veWzOUEoRv06ypFJzXX40";

        // ID чата (группы), куда отправлять файл (можно получить через @getidsbot)
        private static readonly string ChatId = "-1002581439155";

        /// <summary>
        /// Отправляет файл в Telegram-группу.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу.</param>
        /// <returns>True, если отправка успешна.</returns>
        public static async Task<bool> SendFileToTelegramAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл не найден!");
                return false;
            }

            try
            {
                var botClient = new TelegramBotClient(BotToken);

                await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string fileName = Path.GetFileName(filePath);
                    
                    await botClient.SendDocument(
                        chatId: ChatId,
                        document: new InputFileStream(fileStream, fileName),
                        caption: $"Файл: {fileName}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки: {ex.Message}");
                return false;
            }
        }
    }
}


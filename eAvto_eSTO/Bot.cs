using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using eAvto_eSTO.Handlers;
using eAvto_eSTO.Json;

namespace eAvto_eSTO
{
    public static class Bot
    {
        public static TelegramBotClient Client { get; private set; }

        public static async Task RunAsync(CancellationToken cancellationToken)
        {
            await ConfigureAsync();
            await CreateFilesAsync();

            var me = await Client.GetMe(cancellationToken: cancellationToken);
            var updates = await Client.GetUpdates(offset: int.MaxValue, cancellationToken: cancellationToken);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            Client.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
            Console.WriteLine($"Bot {me.Username} woke up.");
        }

        private static async Task ConfigureAsync()
        {
            var rootPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var filePath = rootPath + @"/Config/config.json";
            var config = await JsonReader.ReadJsonAsync<ConfigStructure>(filePath);
            var botCommands = new List<BotCommand>
            {
                new() { Command = "start", Description = "start work" }
            };
            Client = new TelegramBotClient(config.Token ?? throw new InvalidDataException("Token is null."));
            await Client.SetMyCommands(botCommands);
        }

        private static async Task CreateFilesAsync()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var archivePath = Path.Combine(exePath, "Archive");

            if (!Directory.Exists(archivePath))
            {
                await Task.Run(() => Directory.CreateDirectory(archivePath));
            }
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await Handler.HandleMessageAsync(botClient, update);
                    break;
                case UpdateType.CallbackQuery:
                    await Handler.HandleCallbackQueryAsync(botClient, update);
                    break;
                default:
                    break;
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Handler.HandleErrorAsync(botClient, exception);
        }
    }
}


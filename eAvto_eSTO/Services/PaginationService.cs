using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Handlers;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Services
{
    public static class PaginationService
    {
        public static async Task SendUserLicensesAsync(ITelegramBotClient botClient, Update update, int page = 1, 
                                                       int messageId = 0, bool updateButtons = false)
        {
            var userLicenses = await VerificationService.GetUserLicensesAsync();
            var chatId = updateButtons ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            var user = await UserService.GetUserByIdAsync(updateButtons ? update.CallbackQuery.From.Id : update.Message.From.Id);

            if (userLicenses.Count == 0)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.LicensesEmpty);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                return;
            }

            var buttonsPerPage = 5;
            var startIndex = (page - 1) * buttonsPerPage;
            var endIndex = Math.Min(startIndex + buttonsPerPage, userLicenses.Count);
            var buttonsRange = userLicenses.GetRange(startIndex, endIndex - startIndex);

            var navButtons = buttonsRange
            .Select(check => new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(UserService.GetUserByIdAsync(check.UserId).Result.Nickname, $"{CallbackQueryType.UserLicenseCheck}{check.LicenseId}")
            })
            .ToList();

            navButtons.AddRange(GetNavigationButtons(page, endIndex, userLicenses.Count, CallbackQueryType.UserLicensePageCheck.ToString()));

            if (!updateButtons)
            {
                var message = await botClient.SendMessage(
                    chatId: chatId,
                    text: "🗄 Список *заявок* на перевірку ВП:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "🗄 Список *заявок* на перевірку ВП:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
            }
            catch (ApiRequestException)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequestOutdated);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
        }

        public static async Task SendVerificationRequestsAsync(ITelegramBotClient botClient, Update update, int page = 1, 
                                                               int messageId = 0, bool updateButtons = false)
        {
            var verificationRequests = await VerificationService.GetVerificationRequestsAsync();
            var chatId = updateButtons ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            var user = await UserService.GetUserByIdAsync(updateButtons ? update.CallbackQuery.From.Id : update.Message.From.Id);

            if (verificationRequests.Count == 0)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.VerificationRequestsEmpty);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                return;
            }

            var buttonsPerPage = 5;
            var startIndex = (page - 1) * buttonsPerPage;
            var endIndex = Math.Min(startIndex + buttonsPerPage, verificationRequests.Count);
            var buttonsRange = verificationRequests.GetRange(startIndex, endIndex - startIndex);

            var navButtons = buttonsRange
            .Select(request => new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(UserService.GetUserByIdAsync(request.UserId).Result.Nickname, $"{CallbackQueryType.VerificationRequest}{request.RequestId}")
            })
            .ToList();

            navButtons.AddRange(GetNavigationButtons(page, endIndex, verificationRequests.Count, CallbackQueryType.VerificationPageRequest.ToString()));

            if (!updateButtons)
            {
                var message = await botClient.SendMessage(
                    chatId: chatId,
                    text: "📒 Список *заявок* на верифікацію:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "📒 Список *заявок* на верифікацію:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
            }
            catch (ApiRequestException)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequestOutdated);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
        }

        public static async Task SendParkingSpotsAsync(ITelegramBotClient botClient, Update update, int page = 1, 
                                                       int messageId = 0, bool updateButtons = false)
        {
            var chatId = updateButtons ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            var user = await UserService.GetUserByIdAsync(updateButtons ? update.CallbackQuery.From.Id : update.Message.From.Id);
            var carRental = await RentalService.GetLastCarRentalByUserIdAsync(user.UserId);
            var parkingSpots = await RentalService.GetParkingSpotsAsync(carRental != null 
                                                                        && carRental.Status == CarRentalStatusType.Processing 
                                                                        ? carRental.Filter : CarType.None);

            if (parkingSpots.Count == 0)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ParkingSpotsEmpty);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                return;
            }

            var buttonsPerPage = 5;
            var startIndex = (page - 1) * buttonsPerPage;
            var endIndex = Math.Min(startIndex + buttonsPerPage, parkingSpots.Count);
            var buttonsRange = parkingSpots.GetRange(startIndex, endIndex - startIndex);

            var navButtons = buttonsRange
            .Select(spot => new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(spot.Location, $"{CallbackQueryType.CarParkingSpot}{spot.SpotId}")
            })
            .ToList();

            navButtons.AddRange(GetNavigationButtons(page, endIndex, parkingSpots.Count, CallbackQueryType.CarParkingPageSpot.ToString()));

            if (!updateButtons)
            {
                var message = await botClient.SendMessage(
                    chatId: chatId,
                    text: "🅿️ Список *паркінг-спотів*:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "🅿️ Список *паркінг-спотів*:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
            }
            catch (ApiRequestException)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequestOutdated);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
            }
        }

        public static async Task SendCarsAsync(ITelegramBotClient botClient, Update update, int spotId,
                                               int page = 1, int messageId = 0, bool updateButtons = false)
        {
            var chatId = updateButtons ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
            var user = await UserService.GetUserByIdAsync(updateButtons ? update.CallbackQuery.From.Id : update.Message.From.Id);
            var carRental = await RentalService.GetLastCarRentalByUserIdAsync(user.UserId);
            List<Car> cars = [];

            if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
            {
                cars = await RentalService.GetCarsByCarRentalAsync(carRental);
            }
            else
            {
                cars = await RentalService.GetCarsByParkingSpotAsync(spotId);
            }

            if (cars.Count == 0)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.CarsEmpty);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                return;
            }

            var buttonsPerPage = 5;
            var startIndex = (page - 1) * buttonsPerPage;
            var endIndex = Math.Min(startIndex + buttonsPerPage, cars.Count);
            var buttonsRange = cars.GetRange(startIndex, endIndex - startIndex);

            var navButtons = buttonsRange
            .Select(car => new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"{car.Make} {car.Model} {car.Year} {car.Color}", $"{CallbackQueryType.CarSelectedCar}{car.CarId}")
            })
            .ToList();

            navButtons.AddRange(GetNavigationButtons(page, endIndex, cars.Count, CallbackQueryType.CarSelectedPageCar.ToString()));

            if (!updateButtons)
            {
                var message = await botClient.SendMessage(
                    chatId: chatId,
                    text: "🚙 Список *автомобілів* на обраному паркінг-споті:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
                return;
            }

            try
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "🚙 Список *автомобілів* на обраному паркінг-споті:",
                    replyMarkup: new InlineKeyboardMarkup(navButtons),
                    parseMode: ParseMode.Markdown);
            }
            catch (ApiRequestException)
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequestOutdated);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
            }
        }

        private static List<InlineKeyboardButton[]> GetNavigationButtons(int page, int endIndex, int count, string callbackData)
        {
            List<InlineKeyboardButton[]> navButtons = [];

            if (page > 1 && endIndex < count)
            {
                navButtons.Add(
                    [
                        InlineKeyboardButton.WithCallbackData("⏪ Назад", $"{callbackData}{page - 1}"),
                        InlineKeyboardButton.WithCallbackData("Далі ⏩", $"{callbackData}{page - 1}")
                    ]);
            }
            else if (page > 1)
            {
                navButtons.Add([InlineKeyboardButton.WithCallbackData("⏪ Назад", $"{callbackData}{page - 1}")]);
            }
            else if (endIndex < count)
            {
                navButtons.Add([InlineKeyboardButton.WithCallbackData("Далі ⏩", $"{callbackData}{page + 1}")]);
            }

            navButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", $"{callbackData}Back")]);

            return navButtons;
        }
    }
}


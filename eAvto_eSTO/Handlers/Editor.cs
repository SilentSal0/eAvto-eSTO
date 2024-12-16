using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Services;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Handlers
{
    public static class Editor
    {
        public static async Task EditBotMessageAsync(ITelegramBotClient botClient, Update update, BotMessageType botMessageType,
                                                     UserLicense? userLicense = null,
                                                     Car? car = null, CarRental? carRental = null, ParkingSpot? parkingSpot = null)
        {
            long chatId = 0;
            int messageId = 0;
            string text = string.Empty;
            IReplyMarkup? replyMarkup = null;
            ParseMode parseMode = ParseMode.Markdown;

            if (update.Type == UpdateType.Message)
            {
                chatId = update.Message.Chat.Id;
                messageId = update.Message.MessageId;
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                chatId = update.CallbackQuery.Message.Chat.Id;
                messageId = update.CallbackQuery.Message.MessageId;
            }

            switch (botMessageType)
            {
                case BotMessageType.LicenseInformation:
                    text = $"🔤 *Серія ВП*: {userLicense.Series}\n" +
                        $"🔢 *Номер ВП*: {userLicense.Number}";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.LicenseConfirmDiscard);
                    break;

                case BotMessageType.DoYouWantToSelectCarFilter:
                    text = $"❓ Бажаєш обрати *клас* авто для пошуку?";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarFilterYesNo);
                    break;

                case BotMessageType.SelectRentalFilter:
                    text = "🚙 Обери бажаний *клас* авто:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarFilterType);
                    break;

                case BotMessageType.SelectRentalStartDate:
                    text = "📅 Обери бажану *дату початку* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalStartDate);
                    break;

                case BotMessageType.SelectRentalStartTimeToday:
                    text = "🕘 Обери бажаний *час початку* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalStartTimeToday);
                    break;

                case BotMessageType.SelectRentalStartTime24:
                    text = "🕘 Обери бажаний *час початку* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalStartTime24);
                    break;

                case BotMessageType.SelectRentalEndDate:
                    text = "📅 Обери бажану *дату кінця* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalEndDate, update.CallbackQuery.From.Id);
                    break;

                case BotMessageType.SelectRentalEndTimeToday:
                    text = "🕤 Обери бажаний *час кінця* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalEndTimeToday, update.CallbackQuery.From.Id);
                    break;

                case BotMessageType.SelectRentalEndTime24:
                    text = "🕤 Обери бажаний *час кінця* оренди:";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarRentalEndTime24);
                    break;

                case BotMessageType.ParkingSpotAndCarSelected:
                    text = "ℹ️ Інформація про *обране* авто:\n" +
                        $"🅿️ *Паркінг-спот*: {parkingSpot.Name}\n" +
                        $"📍 *Локація*: {parkingSpot.Location}\n" +
                        $"🚙 *Марка*: {car.Make}\n" +
                        $"🆔 *Модель*: {car.Model}\n" +
                        $"📅 *Рік*: {car.Year}\n" +
                        $"🎨 *Колір*: {car.Color}\n" +
                        $"💳 *Ціна (1 год.)*: {car.PricePerHour}₴";

                    if (carRental != null)
                    {
                        replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SelectedCarRentBackMainMenu);
                        break;
                    }

                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SelectedCarBackMainMenu);
                    break;

                case BotMessageType.RentalConfirmation:
                    car = await RentalService.GetCarByIdAsync(carRental.CarId);
                    parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
                    text = "ℹ️ Інформація про *оренду*:\n" +
                        $"🅿️ *Паркінг-спот*: {parkingSpot.Name}\n" +
                        $"📍 *Локація*: {parkingSpot.Location}\n" +
                        $"🚙 *Авто*: {car.Make} {car.Model} {car.Year} {car.Color}\n" +
                        $"⏳ *Дата початку*: {carRental.RentalStart:dd.MM.yyyy HH:mm}\n" +
                        $"⌛️ *Дата кінця*: {carRental.RentalEnd:dd.MM.yyyy HH:mm}\n" +
                        $"💳 *До сплати*: {(int)(carRental.RentalEnd - carRental.RentalStart).TotalHours * car.PricePerHour}₴\n\n" +
                        $"⚠️ *Важливо*: якщо ти *не прибудеш* до авто протягом *15 хвилин* " +
                        $"після обраного *часу початку* оренди - її буде *скасовано*.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.RentalConfirmBackMenu);
                    break;

                case BotMessageType.RentalSuccessful:
                    text = $"👌 Ти *успішно* забронював *{car.Make} {car.Model} {car.Year} {car.Color}*.\n\n" +
                        $"ℹ️ Натисни кнопку «*🔑 Оренда Авто*» для додаткової інформації.";
                    break;
                
                case BotMessageType.RentalEnded:
                    text = $"👌 Ти *успішно* завершив оренду *{car.Make} {car.Model} {car.Year} {car.Color}*.\n\n" +
                        $"💳 Стягнення за *оренду*: {Math.Round((int)(carRental.RentalEnd - carRental.RentalStart).TotalHours
                                                             * car.PricePerHour * 0.8m, 2)}₴\n\n" +
                        $"⚠️ *Важливо*: *покинь* авто протягом *15 хвилин*, щоб *уникнути* проблем.\n" + 
                        $"ℹ️ Як тільки ти *покинеш* авто, ти більше *не зможеш* відчинити його *двері*.";
                    break;

                default:
                    break;
            }

            await botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                replyMarkup: (InlineKeyboardMarkup)replyMarkup,
                parseMode: parseMode);
        }
    }
}


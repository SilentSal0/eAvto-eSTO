using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Services;
using eAvto_eSTO.Enums;
using User = eAvto_eSTO.Databases.User;

namespace eAvto_eSTO.Handlers
{
    public static class Sender
    {
        public static async Task SendBotMessageAsync(ITelegramBotClient botClient, Update update, BotMessageType botMessageType,
                                                     Car? car = null, CarRental? carRental = null, ParkingSpot? parkingSpot = null,
                                                     RegistrationString? registrationString = null, User? user = null, 
                                                     VerificationRequest? verificationRequest = null, string? discardReason = null)
        {
            var me = await botClient.GetMe();
            long chatId = 0;
            string text = string.Empty;
            IReplyMarkup? replyMarkup = null;
            ParseMode parseMode = ParseMode.Markdown;

            if (update.Type == UpdateType.Message)
            {
                chatId = update.Message.Chat.Id;
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                chatId = update.CallbackQuery.Message.Chat.Id;
            }

            switch (botMessageType)
            {
                case BotMessageType.Contacts:
                    text = "🗣 *Контакти*:\n" +
                        "📬 *Email*: eavto.esto@gmail.com\n" +
                        "📞 *Номер*: +380967515075";
                    break;

                case BotMessageType.BadRequest:
                    text = "🥺 Вибач, ми *не* зрозуміли твій запит.";
                    break;

                case BotMessageType.BadRequestOutdated:
                    text = "🥺 Вибач, твій запит *застарілий*.";
                    break;

                case BotMessageType.ImageError:
                    text = "🥺 Вибач, ми *не* змогли отримати фото з бази даних.";
                    break;

                case BotMessageType.PressRegistrationButton:
                    text = "👤 Натисни кнопку «*👤 Реєстрація*» для продовження.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.Registration);
                    break;

                case BotMessageType.PressVerificationButton:
                    text = "👥 Натисни кнопку «*👥 Верифікація*» для продовження.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.Verification);
                    break;

                case BotMessageType.AdminGreeting:
                    text = $"👋 *{user.Nickname}*, обери потрібну дію з меню.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.AdminMenu);
                    break;

                case BotMessageType.UserGreeting:
                    chatId = user.UserId;
                    text = $"👋 *{user.Nickname}*, обери потрібну дію з меню.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.UserMenu);
                    break;

                case BotMessageType.UserIsRegistered:
                    text = "✍️ Для забезпезпечення нашої співпраці, тобі *потрібно* пройти верифікацію.";
                    break;

                case BotMessageType.UserIsNotRegistered:
                    text = $"🙁 Ти ще *не* зареєстрований(-а) у системі «*{me.FirstName}*».";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.Registration);
                    break;

                case BotMessageType.UserIsAlreadyRegistered:
                    text = $"😆 Ти *вже* зареєстрований(-а) у системі «*{me.FirstName}*».";
                    break;

                case BotMessageType.UserIsAlreadyVerified:
                    text = $"😆 Ти *вже* верифікований(-а) у системі «*{me.FirstName}*».";
                    break;

                case BotMessageType.UserEnteredRegistrationStringIncorrectly:
                    text = "🥺 *Дані* введено *неправильно*: має бути `email пароль нікнейм`, *через* пробіл.";
                    break;

                case BotMessageType.UserEnteredEmailIncorrectly:
                    text = "🥺 *Електронна пошта* введена *неправильно*: має містити *@*, *без* пробілів.";
                    break;

                case BotMessageType.UserEnteredPasswordIncorrectly:
                    text = "🥺 *Пароль* введено *неправильно*: має містити *8+ символів*, *без* пробілів.";
                    break;

                case BotMessageType.UserEnteredNicknameIncorrectly:
                    text = "🥺 *Нікнейм* введено *неправильно*: *не* має містити пробілів.";
                    break;

                case BotMessageType.UserEnteredVerificationCodeWrong:
                    text = "😔 *Код підтвердження* введено *неправильно* або *термін його дії сплинув*.\n" +
                        "🤔 *Надіслати* код ще раз чи *перервати* процес реєстрації?";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.VerificationCodeResendCancel);
                    break;

                case BotMessageType.UserEnteredVerificationStringIncorrectly:
                    text = "🥺 *Дані* введено *неправильно*: має бути `серія номер`, *через* пробіл.";
                    break;

                case BotMessageType.UserEnteredLicenseSeriesIncorrectly:
                    text = "🥺 *Серія ВП* введена *неправильно*: має бути *3 латинські букви*, *без* пробілів.";
                    break;

                case BotMessageType.UserEnteredLicenseNumberIncorrectly:
                    text = "🥺 *Номер ВП* введено неправильно: має бути *6 цифр*, *без* пробілів.";
                    break;

                case BotMessageType.UserSentImageIncorrectly:
                    text = "🥺 Ми *не* змогли розпізнати *фото* у повідомленні.\n" +
                        "ℹ️ Надішли фото в одному з форматів: *jpeg*, *jpg*, *png*, *webp*.";
                    break;

                case BotMessageType.UserSentVerificationRequest:
                    text = $"👌 *{user.Nickname}*, ти *успішно* подав(-ла) заявку на верифікацію. Результати *будуть* повідомлені.\n\n" +
                        $"ℹ️ Натиснувши кнопку «*👥 Верифікація*» ще раз, ти *відміниш* поточну заявку.";
                    break;

                case BotMessageType.UserRegisteredSuccessfully:
                    text = $"👤 *{user.Nickname}*, тебе *успішно* зареєстровано у системі «*{me.FirstName}*».";
                    break;

                case BotMessageType.UserVerifiedSuccessfully:
                    chatId = user.UserId;
                    text = $"👥 *{user.Nickname}*, тебе *успішно* верифіковано у системі «*{me.FirstName}*».";
                    break;

                case BotMessageType.UserVerifiedUnsuccessfully:
                    chatId = user.UserId;
                    text = $"😔 *{user.Nickname}*, система «*{me.FirstName}*» *відхилила* твою заявку на верифікацію.\n" +
                        $"📄 *Причина*: {discardReason}.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.Verification);
                    break;

                case BotMessageType.RequestRegistrationString:
                    text = "ℹ️ Надішли наступну *інформацію*:\n" +
                    "📬 *Email*\n" +
                    "🔐 *Пароль*\n" +
                    "👤 *Звернення (нікнейм)*\n" +
                    "📜 Формат: `example@gmail.com qwerty12345 Артур`";
                    break;

                case BotMessageType.RequestRegistrationStringConfirm:
                    text = "🖋️ Перевір, будь ласка, чи все вірно.\n" +
                        $"📬 *Email*: {registrationString.Email}\n" +
                        $"🔐 *Пароль*: ||{registrationString.Password}||\n" +
                        $"👤 *Звернення (нікнейм)*: {registrationString.Nickname}";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.RegistrationYesNo);
                    parseMode = ParseMode.MarkdownV2;
                    break;

                case BotMessageType.RequestVerificationCode:
                    text = "✍️ Введи *код підтвердження*, який ми надіслали на твою *електронну скриньку*.";
                    break;

                case BotMessageType.RequestVerificationCodeResend:
                    text = "✍️ Введи *новий код підтвердження*, який ми надіслали на твою *електронну скриньку*.";
                    break;

                case BotMessageType.RequestVerificationString:
                    text = "ℹ️ Надішли наступну *інформацію*:\n" +
                    "🔤 *Серія водійського посвідчення*\n" +
                    "🔢 *Номер водійського посвідчення*\n" +
                    "📜 Формат: `BXI 861168`";
                    break;

                case BotMessageType.RequestVerificationStringConfirm:
                    text = "🖋️ Перевір, будь ласка, чи все вірно.\n" +
                        $"🔤 *Серія ВП*: {verificationRequest.Series}\n" +
                        $"🔢 *Номер ВП*: {verificationRequest.Number}";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.VerificationYesNo);
                    break;

                case BotMessageType.RequestVerificationDocument:
                    text = "🖼 Надішли *фото лицьової сторони* твого *водійського посвідчення*.\n\n" +
                        "ℹ️ Надсилаючи будь-які фото, ти *погоджуєшся* на *обробку* своїх *персональних даних*.";
                    break;

                case BotMessageType.RequestVerificationSelfie:
                    text = "📸 Надішли *селфі-фото* для підтвердження *особи*. На фото ти маєш *виконати дію* нижче.\n" +
                        $"🧏 *Дія*: `{VerificationActionTypeToString(VerificationService.GenerateVerificationAction())}`";
                    break;

                case BotMessageType.RequestVerificationRequestConfirm:
                    text = "❓ *Підтвердити* чи *відхилити* заявку на верифікацію?";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.VerificationConfirmDiscard);
                    break;

                case BotMessageType.RequestVerificationRequestDiscardReason:
                    text = $"❓ Чому ти *відхиляєш* заявку користувача *{user.Nickname}*?";
                    break;

                case BotMessageType.RegistrationStringIsOutdated:
                    text = "🙁 Дані *застаріли*, процес реєстрації перервано.";
                    break;

                case BotMessageType.RegistrationProcessCancel:
                    text = "🙁 Ти *перервав(-ла)* процес реєстрації.";
                    break;

                case BotMessageType.VerificationStringIsOutdated:
                    text = "🙁 Дані *застаріли*, процес верифікації перервано.";
                    break;

                case BotMessageType.VerificationProcessCancel:
                    text = "🙁 Ти *перервав(-ла)* процес верифікації.";
                    break;

                case BotMessageType.VerificationRequestConfirmed:
                    text = $"✅ Ти *підтвердив(-ла)* заявку на верифікацію користувача *{user.Nickname}*.";
                    break;

                case BotMessageType.VerificationRequestDiscarded:
                    text = $"❌ Ти *відхилив(-ла)* заявку на верифікацію користувача *{user.Nickname}*.";
                    break;

                case BotMessageType.VerificationRequestsEmpty:
                    text = $"🗑 Список *заявок* на верифікацію *пустий*.";
                    break;
                
                case BotMessageType.LicensesEmpty:
                    text = $"🗑 Список *заявок* на перевірку ВП *пустий*.";
                    break;
                
                case BotMessageType.AdminsEmpty:
                    text = $"🗑 Список *адмінів* на даний момент *пустий*.";
                    break;

                case BotMessageType.DoYouWantToSelectCarFilter:
                    text = $"❓ Бажаєш обрати *клас* авто для пошуку?";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.CarFilterYesNo);
                    break;

                case BotMessageType.ParkingSpotsEmpty:
                    text = $"🗑 Список *паркінг-спотів пустий*.";
                    break;

                case BotMessageType.CarsEmpty:
                    text = $"🗑 Список *автомобілів* на обраному паркінг-споті *пустий*.";
                    break;

                case BotMessageType.RentalNoActiveRental:
                    text = $"🧐 Система «*{me.FirstName}*» *не знайшла* заброньованого *авто* на твоє ім'я.";
                    break;
                
                case BotMessageType.RentalTryingToUnlockWithoutConfirmation:
                    text = $"🧐 Система «*{me.FirstName}*» не розглянула твоє *водійське посвідчення*.";
                    break;

                case BotMessageType.RentalInformation:
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
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.ImOnPlaceMainMenu);
                    break;

                case BotMessageType.RentalCancellation:
                    text = $"👌 Ти *успішно* скасував оренду *{car.Make} {car.Model} {car.Year} {car.Color}*.";
                    break;

                case BotMessageType.RentalForcedCancellation:
                    text = $"😔 Ти *не прибув* до авто вчасно, тому оренду *{car.Make} {car.Model} {car.Year} {car.Color}* скасовано.\n" +
                        $"😉 Наступного разу будь більш *відповідальною* людиною.";
                    break;

                case BotMessageType.RentalUserCameTooEarly:
                    text = $"😲 Ти *прибув* до авто завчасно. Тобі треба зачекати ще " +
                        $"*{Math.Abs((int)(DateTime.Now - carRental.RentalStart).TotalMinutes)} хв*.\n" +
                        $"🕘 Максимальний допустимий термін: *15 хв*. до/після *часу початку* оренди.";
                    break;
                
                case BotMessageType.RentalCarUnlockedSuccessfully:
                    text = $"🔑 Автомобіль *успішно* розблоковано.\n\n" +
                        $"💳 Стягнення за *розблокування*: {Math.Round((int)(carRental.RentalEnd - carRental.RentalStart).TotalHours 
                                                             * car.PricePerHour * 0.2m, 2)}₴\n" +
                        $"🕘 *Час початку* оренди: {carRental.RentalStart:dd.MM.yyyy HH:mm}\n" +
                        $"🕞 *Час кінця* оренди: {carRental.RentalEnd:dd.MM.yyyy HH:mm}\n\n" +
                        $"⚠️ *Важливо*: наполегливо просимо *повернути* авто на паркінг-спот вчасно.\n" +
                        $"ℹ️ У випадку, якщо авто *не буде повернено* на паркінг-спот до " +
                        $"*{carRental.RentalEnd.AddMinutes(15):dd.MM.yyyy HH:mm}*, " +
                        $"його буде автоматично *заблоковано*, а ти *отримаєш* штраф.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.EndRent);
                    break;

                case BotMessageType.RentalEnd:
                    if (carRental.RentalEnd > DateTime.Now)
                    {
                        text = $"🕘 У тебе залишилось ще *{(int)(carRental.RentalEnd - DateTime.Now).TotalMinutes} хв*. оренди.\n" +
                            $"❓ Завершити *оренду* зараз?";
                    }
                    else
                    {
                        text = $"❓ Завершити *оренду*?";
                    }
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.EndRentYesNo);
                    break;

                case BotMessageType.WaitForLicenseVerification:
                    text = $"⏳ Зачекай хвилинку, ми *перевіримо* твоє *водійське посвідчення*.\n" +
                        $"🕘 Час, потрачений на *перевірку*, буде *додано* до твоєї *оренди*.";
                    break;

                case BotMessageType.LicenseVerifiedSuccessfully:
                    chatId = user.UserId;
                    text = $"👌 *{user.Nickname}*, твоє водійське посвідчення *успішно* перевірено.\n\n" +
                        $"🔐 Натисни кнопку «*💳 Розблокувати Авто*» для початку оренди.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.UnlockCar);
                    break;

                case BotMessageType.LicenseVerifiedUnsuccessfully:
                    chatId = user.UserId;
                    text = $"😔 *{user.Nickname}*, система «*{me.FirstName}*» *відхилила* твоє водійське посвідчення.\n" +
                        $"📄 *Причина*: водійське посвідчення більше *не* дійсне.\n\n" +
                        $"ℹ️ Це означає, що тобі доведеться повторно пройти *верифікацію*.\n" +
                        $"😳 Якщо ти вважаєш, що сталася *помилка* - повідом нам.";
                        
                    break;

                case BotMessageType.LicenseCheckConfirmed:
                    text = $"✅ Ти *підтвердив(-ла)* заявку на перевірку ВП користувача *{user.Nickname}*.";
                    break;

                case BotMessageType.LicenseCheckDiscarded:
                    text = $"❌ Ти *відхилив(-ла)* заявку на перевірку ВП користувача *{user.Nickname}*.";
                    break;

                case BotMessageType.ChooseSetting:
                    text = $"🛠 Обери потрібну дію з меню.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.Settings);
                    break;
                
                case BotMessageType.RequestNewNickname:
                    text = $"✍️ Введи *новий нікнейм*.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SettingsBack);
                    break;

                case BotMessageType.RequestNewPassword:
                    text = $"✍️ Введи *новий пароль*.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SettingsBack);
                    break;

                case BotMessageType.RequestVerificationCodePassword:
                    text = "✍️ Введи *код підтвердження*, який ми надіслали на твою *електронну скриньку*.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SettingsBack);
                    break;

                case BotMessageType.RequestAccountRemovalConfirmation:
                    text = "✍️ Надішли повідомлення «*ВИДАЛИТИ*» для підтвердження *видалення аккаунту*.";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.SettingsBack);
                    break;

                case BotMessageType.UserEnteredVerificationCodePasswordWrong:
                    text = "😔 *Код підтвердження* введено *неправильно* або *термін його дії сплинув*.\n" +
                        "🤔 *Надіслати* код ще раз чи *перервати* процес зміни пароля?";
                    replyMarkup = await ReplyMarkup.GetReplyMarkup(ReplyMarkupType.VerificationCodeSettingsResendCancel);
                    break;

                case BotMessageType.UserEnteredAccountRemovalConfirmationWrong:
                    text = "🙅 Повідомлення не відповідає слову «*ВИДАЛИТИ*».\n" +
                        "🙂 Процес видалення аккаунту перервано.";
                    break;

                case BotMessageType.UserChangedNicknameSuccessfully:
                    text = "👤 Ти *успішно* змінив(-ла) свій *нікнейм*.";
                    break;
                
                case BotMessageType.UserChangedPasswordSuccessfully:
                    text = "🔐 Ти *успішно* змінив(-ла) свій *пароль*.";
                    break;

                case BotMessageType.UserRemovedAccountSuccessfully:
                    text = "🚮 Ти *успішно* видалив(-лила) свій *аккаунт*.";
                    break;

                default:
                    break;
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: parseMode == ParseMode.MarkdownV2 ? EscapeMarkdownV2(text) : text,
                replyMarkup: replyMarkup,
                parseMode: parseMode);
        }

        private static string EscapeMarkdownV2(string text)
        {
            var chars = new string[] { "-", "_", ".", "!", "(", ")", "[", "]", "{", "}", "#", "+", "=", "~", ">" };

            foreach (var c in chars)
            {
                text = text.Replace(c, $"\\{c}");
            }

            return text;
        }

        private static string VerificationActionTypeToString(VerificationActionType verificationActionType)
        {
            return verificationActionType switch
            {
                VerificationActionType.None => "None",
                VerificationActionType.Smile => "посміхнутись",
                VerificationActionType.Wink => "підморгнути",
                VerificationActionType.Tongue => "показати язик",
                _ => "None"
            };
        }
    }
}


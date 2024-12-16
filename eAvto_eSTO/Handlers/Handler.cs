using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Services;
using eAvto_eSTO.Enums;
using File = Telegram.Bot.Types.File;
using User = eAvto_eSTO.Databases.User;

namespace eAvto_eSTO.Handlers
{
    public static class Handler
    {
        private static List<int> _messageIds = [];
        private static string _previousMessage = string.Empty;

        public static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message is not { } message) return;

            if (_previousMessage.Equals(CallbackQueryType.VerificationStringYes.ToString()) || _previousMessage.Equals(BotMessageType.RequestVerificationSelfie.ToString()))
            {
                await HandleVerificationImagesAsync(botClient, update, message);
                return;
            }

            if (message.Text is not { } messageText) return;

            var userId = message.From.Id;

            if (UserService.IsUserAdmin(userId))
            {
                await HandleAdminAsync(botClient, update, userId, messageText);
            }
            else
            {
                await HandleUserAsync(botClient, update, userId, messageText);
            }
        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.CallbackQuery is not { } callbackQuery) return;

            var data = callbackQuery.Data;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var userId = callbackQuery.From.Id;
            var spotId = 0;
            Car? car;
            CarRental? carRental;
            RegistrationString? registrationString;
            User? user;
            UserLicense? userLicense;
            VerificationCode verificationCode;
            VerificationRequest? verificationRequest;

            if (data.StartsWith(CallbackQueryType.UserLicensePageCheck.ToString()) && !data.Contains("Back"))
            {
                var page = int.Parse(data.Replace(CallbackQueryType.UserLicensePageCheck.ToString(), ""));
                await PaginationService.SendUserLicensesAsync(botClient, update, page, messageId);
                return;
            }

            if (data.StartsWith(CallbackQueryType.UserLicenseCheck.ToString()))
            {
                var licenseId = int.Parse(data.Replace(CallbackQueryType.UserLicenseCheck.ToString(), ""));
                _previousMessage = licenseId.ToString();
                userLicense = await VerificationService.GetUserLicenseByIdAsync(licenseId);
                await Editor.EditBotMessageAsync(botClient, update, BotMessageType.LicenseInformation, userLicense);
                return;
            }

            if (data.StartsWith(CallbackQueryType.VerificationPageRequest.ToString()) && !data.Contains("Back"))
            {
                var page = int.Parse(data.Replace(CallbackQueryType.VerificationPageRequest.ToString(), ""));
                await PaginationService.SendVerificationRequestsAsync(botClient, update, page, messageId);
                return;
            }

            if (data.StartsWith(CallbackQueryType.VerificationRequest.ToString()))
            {
                var requestId = int.Parse(data.Replace(CallbackQueryType.VerificationRequest.ToString(), ""));
                _previousMessage = requestId.ToString();
                verificationRequest = await VerificationService.GetVerificationRequestByIdAsync(requestId);
                user = await UserService.GetUserByIdAsync(userId);

                var caption = $"🔤 *Серія ВП*: {verificationRequest.Series}\n🔢 *Номер ВП*: {verificationRequest.Number}";
                using var documentStream = new MemoryStream(verificationRequest.Document);
                using var selfieStream = new MemoryStream(verificationRequest.Selfie);
                var media = new List<IAlbumInputMedia>
                {
                    new InputMediaPhoto(new InputFileStream(documentStream, "document")) { Caption = caption, ParseMode = ParseMode.Markdown },
                    new InputMediaPhoto(new InputFileStream(selfieStream, "selfie"))
                };

                await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);

                try
                {
                    var messageGroup = await botClient.SendMediaGroup(
                    chatId: callbackQuery.Message.Chat.Id,
                    media: media);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationRequestConfirm);

                    _messageIds = messageGroup.Select(m => m.MessageId).ToList();
                    _messageIds.Add(callbackQuery.Message.MessageId + 3);
                }
                catch (Exception)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ImageError);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                }
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarRentalStartDate.ToString()) && !data.Contains("Back"))
            {
                var days = int.Parse(data.Replace(CallbackQueryType.CarRentalStartDate.ToString(), ""));
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                carRental.RentalStart = DateTime.Today.AddDays(days);
                carRental.RentalEnd = carRental.RentalStart;
                await RentalService.UpdateCarRentalAsync(carRental);

                if (carRental.RentalStart.Date == DateTime.Today.Date)
                {
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartTimeToday);
                    return;
                }

                await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartTime24);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarRentalStartTime.ToString()) && !data.Contains("Back"))
            {
                var hours = int.Parse(data.Replace(CallbackQueryType.CarRentalStartTime.ToString(), ""));
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                carRental.RentalStart = new DateTime(carRental.RentalStart.Year, carRental.RentalStart.Month, carRental.RentalStart.Day);
                await RentalService.UpdateCarRentalByUserIdAsync(userId, rentalStart: carRental.RentalStart.AddHours(hours));
                await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndDate);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarRentalEndDate.ToString()) && !data.Contains("Back"))
            {
                var days = int.Parse(data.Replace(CallbackQueryType.CarRentalEndDate.ToString(), ""));
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                carRental.RentalEnd = carRental.RentalStart.AddDays(days);
                await RentalService.UpdateCarRentalAsync(carRental);

                if (carRental.RentalStart.Date == carRental.RentalEnd.Date)
                {
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndTimeToday);
                    return;
                }

                await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndTime24);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarRentalEndTime.ToString()) && !data.Contains("Back"))
            {
                var hours = int.Parse(data.Replace(CallbackQueryType.CarRentalEndTime.ToString(), ""));
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                carRental.RentalEnd = new DateTime(carRental.RentalEnd.Year, carRental.RentalEnd.Month, carRental.RentalEnd.Day);
                await RentalService.UpdateCarRentalByUserIdAsync(userId, rentalEnd: carRental.RentalEnd.AddHours(hours));
                await PaginationService.SendParkingSpotsAsync(botClient, update, 1, messageId, true);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarParkingPageSpot.ToString()) && !data.Contains("Back"))
            {
                var page = int.Parse(data.Replace(CallbackQueryType.CarParkingPageSpot.ToString(), ""));
                await PaginationService.SendParkingSpotsAsync(botClient, update, page, messageId, true);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarParkingSpot.ToString()))
            {
                spotId = int.Parse(data.Replace(CallbackQueryType.CarParkingSpot.ToString(), ""));
                _previousMessage = spotId.ToString();
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
                {
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, spotId: spotId);
                }

                await PaginationService.SendCarsAsync(botClient, update, spotId, 1, messageId, true);

                var cars = await RentalService.GetCarsByParkingSpotAsync(spotId);

                if (cars.Count == 0)
                {
                    await botClient.DeleteMessage(update.Message.Chat.Id, update.Message.MessageId);
                }
                return;
            }
            
            if (data.StartsWith(CallbackQueryType.CarSelectedPageCar.ToString()) && !data.Contains("Back"))
            {
                spotId = int.Parse(_previousMessage);
                var page = int.Parse(data.Replace(CallbackQueryType.CarSelectedPageCar.ToString(), ""));
                await PaginationService.SendCarsAsync(botClient, update, spotId, page, messageId, true);
                return;
            }

            if (data.StartsWith(CallbackQueryType.CarSelectedCar.ToString()) && !data.Contains("Back") && !data.Contains("Rent"))
            {
                spotId = int.Parse(_previousMessage);
                var carId = int.Parse(data.Replace(CallbackQueryType.CarSelectedCar.ToString(), ""));
                var parkingSpot = await RentalService.GetParkingSpotByIdAsync(spotId);
                car = await RentalService.GetCarByIdAsync(carId);
                carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
                {
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, carId: carId);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.ParkingSpotAndCarSelected, 
                                                     car: car, carRental: carRental, parkingSpot: parkingSpot);
                    return;
                }

                await Editor.EditBotMessageAsync(botClient, update, BotMessageType.ParkingSpotAndCarSelected, 
                                                 car: car, parkingSpot: parkingSpot);
                return;
            }

            switch (data)
            {
                case nameof(CallbackQueryType.MainMenu):
                    _previousMessage = CallbackQueryType.MainMenu.ToString();
                    user = await UserService.GetUserByIdAsync(userId);

                    if (!UserService.IsUserAdmin(userId))
                    {
                        await DeleteOldMessages(botClient, update, chatId);

                        carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                        if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
                        {
                            await RentalService.RemoveCarRentalByUserIdAsync(userId);
                        }

                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                        break;
                    }

                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.RegistrationStringYes):
                    _previousMessage = CallbackQueryType.RegistrationStringYes.ToString();
                    registrationString = await RegistrationService.GetRegistrationStringByUserIdAsync(userId);
                    verificationCode = VerificationCodeService.GenerateVerificationCode();
                    verificationCode.UserId = userId;

                    _messageIds.Add(callbackQuery.Message.MessageId);
                    await DeleteOldMessages(botClient, update, chatId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationCode);
                    await VerificationCodeService.SaveVerificationCodeAsync(verificationCode);
                    await EmailService.SendVerificationCodeAsync(registrationString.Email, verificationCode);
                    break;

                case nameof(CallbackQueryType.RegistrationStringNo):
                    _previousMessage = CallbackQueryType.RegistrationStringNo.ToString();

                    _messageIds.Add(callbackQuery.Message.MessageId);
                    await DeleteOldMessages(botClient, update, chatId);
                    await RegistrationService.RemoveRegistrationStringByUserIdAsync(userId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RegistrationProcessCancel);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    break;

                case nameof(CallbackQueryType.VerificationCodeResend):
                    _previousMessage = CallbackQueryType.VerificationCodeResend.ToString();
                    registrationString = await RegistrationService.GetRegistrationStringByUserIdAsync(userId);
                    user = await UserService.GetUserByIdAsync(userId);
                    verificationCode = VerificationCodeService.GenerateVerificationCode();
                    verificationCode.UserId = userId;
                    var email = registrationString == null ? user.Email : registrationString.Email;

                    await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationCodeResend);
                    await VerificationCodeService.SaveVerificationCodeAsync(verificationCode);
                    await EmailService.SendVerificationCodeAsync(email, verificationCode);
                    break;

                case nameof(CallbackQueryType.VerificationCodeCancel):
                    _previousMessage = CallbackQueryType.VerificationCodeCancel.ToString();

                    await RegistrationService.RemoveRegistrationStringByUserIdAsync(userId);
                    await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RegistrationProcessCancel);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    break;

                case nameof(CallbackQueryType.VerificationCodeSettingsCancel):
                    _previousMessage = CallbackQueryType.VerificationCodeSettingsCancel.ToString();

                    await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
                    break;

                case nameof(CallbackQueryType.VerificationStringYes):
                    _previousMessage = CallbackQueryType.VerificationStringYes.ToString();

                    await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationDocument);
                    break;

                case nameof(CallbackQueryType.VerificationStringNo):
                    _previousMessage = CallbackQueryType.VerificationStringNo.ToString();

                    await VerificationService.RemoveVerificationRequestByUserIdAsync(userId);
                    await botClient.DeleteMessage(chatId, callbackQuery.Message.MessageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.VerificationProcessCancel);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    break;

                case nameof(CallbackQueryType.VerificationConfirm):
                    verificationRequest = await VerificationService.GetVerificationRequestByIdAsync(int.Parse(_previousMessage));
                    user = await UserService.GetUserByIdAsync(verificationRequest.UserId);
                    _previousMessage = CallbackQueryType.VerificationConfirm.ToString();

                    await VerificationService.VerifyUserAsync(user.UserId);
                    await ArchiveService.ArchiveVerificationRequestAsync(verificationRequest);
                    await VerificationService.RemoveVerificationRequestByUserIdAsync(user.UserId);

                    foreach (var msgId in _messageIds)
                    {
                        await botClient.DeleteMessage(chatId, msgId);
                    }

                    _messageIds.Clear();
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserVerifiedSuccessfully, user: user);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.VerificationRequestConfirmed, user: user);

                    user = await UserService.GetUserByIdAsync(userId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.VerificationDiscard):
                    verificationRequest = await VerificationService.GetVerificationRequestByIdAsync(int.Parse(_previousMessage));
                    _previousMessage += ":" + CallbackQueryType.VerificationDiscard.ToString();
                    user = await UserService.GetUserByIdAsync(verificationRequest.UserId);

                    foreach (var msgId in _messageIds)
                    {
                        await botClient.DeleteMessage(chatId, msgId);
                    }

                    _messageIds.Clear();
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationRequestDiscardReason, user: user);
                    break;

                case nameof(CallbackQueryType.LicenseConfirm):
                    userLicense = await VerificationService.GetUserLicenseByIdAsync(int.Parse(_previousMessage));
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userLicense.UserId);
                    user = await UserService.GetUserByIdAsync(userLicense.UserId);
                    _previousMessage = CallbackQueryType.LicenseConfirm.ToString();

                    await VerificationService.UpdateUserLicenseStatusByUserIdAsync(user.UserId, true);
                    await RentalService.UpdateCarRentalByUserIdAsync(user.UserId, 
                                                                     rentalEnd: carRental.RentalEnd.AddMinutes(
                                                                         (DateTime.Now - carRental.RentalStart).TotalMinutes));

                    await botClient.DeleteMessage(chatId, messageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.LicenseVerifiedSuccessfully, user: user);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.LicenseCheckConfirmed, user: user);

                    user = await UserService.GetUserByIdAsync(userId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.LicenseDiscard):
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                    userLicense = await VerificationService.GetUserLicenseByIdAsync(int.Parse(_previousMessage));
                    user = await UserService.GetUserByIdAsync(userLicense.UserId);
                    _previousMessage = CallbackQueryType.LicenseDiscard.ToString();

                    await VerificationService.UnverifyUserAsync(user.UserId);
                    await RentalService.UpdateCarRentalByUserIdAsync(user.UserId, status: CarRentalStatusType.Canceled);

                    await botClient.DeleteMessage(chatId, messageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.LicenseVerifiedUnsuccessfully, user: user);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.Contacts);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.LicenseCheckDiscarded, user: user);

                    user = await UserService.GetUserByIdAsync(userId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.VerificationPageRequestBack):
                    _previousMessage = CallbackQueryType.VerificationPageRequestBack.ToString();
                    user = await UserService.GetUserByIdAsync(userId);
                    await DeleteOldMessages(botClient, update, chatId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;
                    
                case nameof(CallbackQueryType.UserLicensePageCheckBack):
                    _previousMessage = CallbackQueryType.UserLicensePageCheckBack.ToString();
                    user = await UserService.GetUserByIdAsync(userId);
                    await DeleteOldMessages(botClient, update, chatId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.CarFilterYes):
                    _previousMessage = CallbackQueryType.CarFilterYes.ToString();
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalFilter);
                    break;

                case nameof(CallbackQueryType.CarFilterNo):
                    _previousMessage = CallbackQueryType.CarFilterNo.ToString();
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, filter: CarType.None);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartDate);
                    break;

                case nameof(CallbackQueryType.CarFilterEconom):
                    _previousMessage = CallbackQueryType.CarFilterEconom.ToString();
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, filter: CarType.Econom);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartDate);
                    break;

                case nameof(CallbackQueryType.CarFilterStandard):
                    _previousMessage = CallbackQueryType.CarFilterStandard.ToString();
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, filter: CarType.Standard);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartDate);
                    break;

                case nameof(CallbackQueryType.CarFilterPremium):
                    _previousMessage = CallbackQueryType.CarFilterPremium.ToString();
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, filter: CarType.Premium);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartDate);
                    break;

                case nameof(CallbackQueryType.CarFilterBack):
                    _previousMessage = CallbackQueryType.CarFilterBack.ToString();
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.DoYouWantToSelectCarFilter);
                    break;

                case nameof(CallbackQueryType.CarRentalStartDateBack):
                    _previousMessage = CallbackQueryType.CarRentalStartDateBack.ToString();
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                    if (carRental.Filter == CarType.None)
                    {
                        await Editor.EditBotMessageAsync(botClient, update, BotMessageType.DoYouWantToSelectCarFilter);
                        return;
                    }

                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalFilter);
                    break;

                case nameof(CallbackQueryType.CarRentalStartTimeBack):
                    _previousMessage = CallbackQueryType.CarRentalStartTimeBack.ToString();
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartDate);
                    break;

                case nameof(CallbackQueryType.CarRentalEndDateBack):
                    _previousMessage = CallbackQueryType.CarRentalEndDateBack.ToString();
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                    if (carRental.RentalStart.Date == DateTime.Today.Date)
                    {
                        await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartTimeToday);
                        return;
                    }

                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalStartTime24);
                    break;

                case nameof(CallbackQueryType.CarRentalEndTimeBack):
                    _previousMessage = CallbackQueryType.CarRentalEndTimeBack.ToString();
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndDate);
                    break;

                case nameof(CallbackQueryType.CarParkingPageSpotBack):
                    _previousMessage = CallbackQueryType.CarParkingPageSpotBack.ToString();
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                    if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
                    {
                        if (carRental.RentalStart.Date == carRental.RentalEnd.Date)
                        {
                            await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndTimeToday);
                            return;
                        }

                        await Editor.EditBotMessageAsync(botClient, update, BotMessageType.SelectRentalEndTime24);
                        return;
                    }

                    user = await UserService.GetUserByIdAsync(userId);

                    await DeleteOldMessages(botClient, update, chatId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    break;

                case nameof(CallbackQueryType.CarSelectedPageCarBack):
                    _previousMessage = CallbackQueryType.CarSelectedPageCarBack.ToString();
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                    if (carRental != null)
                    {
                        await RentalService.UpdateCarRentalByUserIdAsync(userId, spotId: 0);
                    }

                    await PaginationService.SendParkingSpotsAsync(botClient, update, 1, messageId, true);
                    break;

                case nameof(CallbackQueryType.CarSelectedCarRent):
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.RentalConfirmation, carRental: carRental);
                    break;

                case nameof(CallbackQueryType.CarSelectedCarBack):
                    spotId = int.Parse(_previousMessage);
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                    if (carRental != null)
                    {
                        await RentalService.UpdateCarRentalByUserIdAsync(userId, carId: 0);
                    }

                    await PaginationService.SendCarsAsync(botClient, update, spotId, 1, messageId, true);
                    break;

                case nameof(CallbackQueryType.CarRentalRent):
                    user = await UserService.GetUserByIdAsync(userId);
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                    car = await RentalService.GetCarByIdAsync(carRental.CarId);

                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.RentalSuccessful, car: car);
                    await RentalService.UpdateCarRentalByUserIdAsync(userId, status: CarRentalStatusType.Confirmed);
                    await EmailService.SendCarRentalConfirmationAsync(user.Email, carRental);
                    break;      

                case nameof(CallbackQueryType.CarRentalBack):
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                    var parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
                    car = await RentalService.GetCarByIdAsync(carRental.CarId);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.ParkingSpotAndCarSelected, 
                                                     car: car, carRental: carRental, parkingSpot: parkingSpot);
                    break;

                case nameof(CallbackQueryType.CarRentalEndYes):
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                    car = await RentalService.GetCarByIdAsync(carRental.CarId);
                    user = await UserService.GetUserByIdAsync(userId);
                    await Editor.EditBotMessageAsync(botClient, update, BotMessageType.RentalEnded, car: car, carRental: carRental);
                    await EmailService.SendCarRentalEndAsync(user.Email, carRental);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    await RentalService.UpdateCarRentalByUserIdAsync(userId,
                                                 rentalEnd: DateTime.Now,
                                                 status: CarRentalStatusType.Completed);
                    break;

                case nameof(CallbackQueryType.CarRentalEndNo):
                    await botClient.DeleteMessage(chatId, messageId);
                    break;

                case nameof(CallbackQueryType.SettingsBack):
                    _previousMessage = CallbackQueryType.SettingsBack.ToString();
                    await botClient.DeleteMessage(chatId, messageId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
                    break;

                default:
                    break;
            }
        }

        private static async Task HandleAdminAsync(ITelegramBotClient botClient, Update update, long userId, string messageText)
        {
            if (messageText.Equals(ButtonTypeToString(ButtonType.Start), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Start);
                var user = await UserService.GetUserByIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.LicenseCheck), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.LicenseCheck);
                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);
                await PaginationService.SendUserLicensesAsync(botClient, update);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.VerificationRequests), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.VerificationRequests);
                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);
                await PaginationService.SendVerificationRequestsAsync(botClient, update);
            }
            else if (_previousMessage.Contains(CallbackQueryType.VerificationDiscard.ToString()))
            {
                var requestId = int.Parse(_previousMessage.Split(":")[0]);
                var verificationRequest = await VerificationService.GetVerificationRequestByIdAsync(requestId);
                var user = await UserService.GetUserByIdAsync(verificationRequest.UserId);
                _previousMessage = messageText;

                await ArchiveService.ArchiveVerificationRequestAsync(verificationRequest, messageText);
                await VerificationService.RemoveVerificationRequestByUserIdAsync(user.UserId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserVerifiedUnsuccessfully, user: user, discardReason: messageText);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.VerificationRequestDiscarded, user: user);

                user = await UserService.GetUserByIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
            else
            {
                _previousMessage = messageText;
                var user = await UserService.GetUserByIdAsync(userId);

                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequest);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
        }

        private static async Task HandleUserAsync(ITelegramBotClient botClient, Update update, long userId, string messageText)
        {
            if (!UserService.IsUserRegistered(userId))
            {
                await HandleRegistrationAsync(botClient, update, userId, messageText);
            }
            else if (UserService.IsUserRegistered(userId) && !UserService.IsUserVerified(userId))
            {
                await HandleVerificationAsync(botClient, update, userId, messageText);
            }
            else if (UserService.IsUserVerified(userId))
            {
                await HandleUserActionsAsync(botClient, update, userId, messageText);
            }
        }

        private static async Task HandleRegistrationAsync(ITelegramBotClient botClient, Update update, long userId, string messageText)
        {
            if (messageText.Equals(ButtonTypeToString(ButtonType.Start), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Start);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsNotRegistered);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Registration), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Registration);

                if (await RegistrationService.GetRegistrationStringByUserIdAsync(userId) != null)
                {
                    await RegistrationService.RemoveRegistrationStringByUserIdAsync(userId);
                }

                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestRegistrationString);
            }
            else if (_previousMessage.Contains(ButtonTypeToString(ButtonType.Registration)))
            {
                _previousMessage = messageText;
                var pattern = @"^([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\s([^\s]{8,})\s([^\s]+)$";
                var parts = messageText.Split(' ');

                if (Regex.IsMatch(messageText, pattern))
                {
                    var registrationString = new RegistrationString
                    {
                        UserId = userId,
                        Email = parts[0].Trim(),
                        Password = parts[1].Trim(),
                        Nickname = parts[2].Trim()
                    };

                    _messageIds.Add(update.Message.MessageId);
                    _messageIds.Add(update.Message.MessageId + 1);
                    await RegistrationService.SaveRegistrationStringAsync(registrationString);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestRegistrationStringConfirm, registrationString: registrationString);
                    return;
                }

                if (parts.Length != 3)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredRegistrationStringIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    return;
                }

                var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                var passwordPattern = @"^[^\s]{8,}$";
                var namePattern = @"^[^\s]+$";

                if (!Regex.IsMatch(parts[0], emailPattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredEmailIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    return;
                }

                if (!Regex.IsMatch(parts[1], passwordPattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredPasswordIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    return;
                }

                if (!Regex.IsMatch(parts[2], namePattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredNicknameIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    return;
                }
            }
            else if (_previousMessage.Equals(CallbackQueryType.RegistrationStringYes.ToString()) 
                     || _previousMessage.Equals(CallbackQueryType.VerificationCodeResend.ToString()))
            {
                _previousMessage = messageText;

                if (int.TryParse(messageText, out var code))
                {
                    if (!VerificationCodeService.IsVerificationCodeValid(userId, code))
                    {
                        _messageIds.Add(update.Message.MessageId + 1);
                        await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredVerificationCodeWrong);
                        return;
                    }

                    await RegistrationService.RegisterUserAsync(userId);
                    await RegistrationService.RemoveRegistrationStringByUserIdAsync(userId);
                    await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);

                    var user = await UserService.GetUserByIdAsync(userId);

                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserRegisteredSuccessfully, user: user);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsRegistered);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    return;
                }

                _messageIds.Add(update.Message.MessageId + 1);
                await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredVerificationCodeWrong);
            }
            else
            {
                _previousMessage = messageText;

                if (await RegistrationService.GetRegistrationStringByUserIdAsync(userId) != null)
                {
                    await RegistrationService.RemoveRegistrationStringByUserIdAsync(userId);
                }

                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequest);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
            }
        }

        private static async Task HandleVerificationAsync(ITelegramBotClient botClient, Update update, long userId, string messageText)
        {
            if (messageText.Equals(ButtonTypeToString(ButtonType.Start), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Start);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsRegistered);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Registration), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Registration);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsAlreadyRegistered);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsRegistered);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Verification), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Verification);

                if (await VerificationService.GetVerificationRequestByUserIdAsync(userId) != null)
                {
                    await VerificationService.RemoveVerificationRequestByUserIdAsync(userId);
                }

                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationString);
            }
            else if (_previousMessage.Contains(ButtonTypeToString(ButtonType.Verification)))
            {
                _previousMessage = messageText;
                var pattern = @"^[a-zA-Zа-яА-Я]{3}\s[0-9]{6}$";
                var parts = messageText.Split(' ');

                if (Regex.IsMatch(messageText, pattern))
                {
                    var verificationRequest = new VerificationRequest
                    {
                        UserId = userId,
                        Series = parts[0].Trim().ToUpper(),
                        Number = parts[1].Trim(),
                        Date = DateTime.Now
                    };

                    _messageIds.Add(update.Message.MessageId + 1);
                    await VerificationService.SaveVerificationRequestAsync(verificationRequest);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationStringConfirm, verificationRequest: verificationRequest);
                    return;
                }

                if (parts.Length != 2)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredVerificationStringIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    return;
                }

                var seriesPattern = @"^[a-zA-Zа-яА-Я]{3}$";
                var numberPattern = @"^[0-9]{6}$";

                if (!Regex.IsMatch(parts[0], seriesPattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredLicenseSeriesIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    return;
                }

                if (!Regex.IsMatch(parts[1], numberPattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredLicenseNumberIncorrectly);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
                    return;
                }
            }
            else
            {
                _previousMessage = messageText;

                if (await VerificationService.GetVerificationRequestByUserIdAsync(userId) != null)
                {
                    await VerificationService.RemoveVerificationRequestByUserIdAsync(userId);
                }

                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.BadRequest);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressVerificationButton);
            }
        }

        private static async Task HandleVerificationImagesAsync(ITelegramBotClient botClient, Update update, Message message)
        {
            var userId = message.From.Id;
            File? file = null;

            if (message.Text != null || (message.Photo == null && message.Document == null))
            {
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserSentImageIncorrectly);
                return;
            }

            if (message.Photo != null && message.Photo.Length != 0)
            {
                var photo = message.Photo.Last();
                file = await botClient.GetFile(photo.FileId);
            }
            else if (message.Document != null)
            {
                var supportedImageMimeTypes = new HashSet<string>
                {
                    "image/jpeg",
                    "image/jpg",
                    "image/png",
                    "image/webp"
                };

                if (supportedImageMimeTypes.Contains(message.Document.MimeType))
                {
                    file = await botClient.GetFile(message.Document.FileId);
                }
                else
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserSentImageIncorrectly);
                    return;
                }
            }

            if (_previousMessage.Equals(CallbackQueryType.VerificationStringYes.ToString()))
            {
                _previousMessage = BotMessageType.RequestVerificationSelfie.ToString();
                var verificationRequest = await VerificationService.GetVerificationRequestByUserIdAsync(userId);
                var document = await VerificationService.DownloadImageAsync(botClient, file);

                await VerificationService.UpdateVerificationRequestWithImageAsync(verificationRequest, document);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationSelfie);
            }
            else if (_previousMessage.Equals(BotMessageType.RequestVerificationSelfie.ToString()))
            {
                _previousMessage = BotMessageType.UserSentVerificationRequest.ToString();
                var verificationRequest = await VerificationService.GetVerificationRequestByUserIdAsync(userId);
                var selfie = await VerificationService.DownloadImageAsync(botClient, file);
                var user = await UserService.GetUserByIdAsync(userId);

                await VerificationService.UpdateVerificationRequestWithImageAsync(verificationRequest, selfie);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserSentVerificationRequest, user: user);
            }
        }

        private static async Task HandleUserActionsAsync(ITelegramBotClient botClient, Update update, long userId, string messageText)
        {
            if (messageText.Equals(ButtonTypeToString(ButtonType.Start), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Start);
                var user = await UserService.GetUserByIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.MainMenu), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.MainMenu);
                var user = await UserService.GetUserByIdAsync(userId);

                await DeleteOldMessages(botClient, update);

                if (!UserService.IsUserAdmin(userId))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminGreeting, user: user);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Settings), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Settings);

                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.ChangeNickname), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.ChangeNickname);
                _messageIds.Clear();
                _messageIds.Add(update.Message.MessageId + 1);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestNewNickname);
            }
            else if (_previousMessage.Equals(ButtonTypeToString(ButtonType.ChangeNickname)))
            {
                var namePattern = @"^[^\s]+$";

                if (!Regex.IsMatch(messageText, namePattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredNicknameIncorrectly);
                    return;
                }

                _previousMessage = messageText;
                await UserService.UpdateUserByIdAsync(userId, nickname: messageText);
                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserChangedNicknameSuccessfully);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.ChangePassword), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = BotMessageType.RequestVerificationCodePassword.ToString();
                var user = await UserService.GetUserByIdAsync(userId);
                var verificationCode = VerificationCodeService.GenerateVerificationCode();
                verificationCode.UserId = userId;

                _messageIds.Clear();
                _messageIds.Add(update.Message.MessageId + 1);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestVerificationCodePassword);
                await VerificationCodeService.SaveVerificationCodeAsync(verificationCode);
                await EmailService.SendVerificationCodeAsync(user.Email, verificationCode);
            }
            else if (_previousMessage.Equals(BotMessageType.RequestVerificationCodePassword.ToString())
                     || _previousMessage.Equals(CallbackQueryType.VerificationCodeResend.ToString()))
            {
                _previousMessage = messageText;
                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);

                if (int.TryParse(messageText, out var code))
                {
                    if (!VerificationCodeService.IsVerificationCodeValid(userId, code))
                    {
                        await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredVerificationCodePasswordWrong);
                        return;
                    }

                    _previousMessage = ButtonTypeToString(ButtonType.ChangePassword);
                    _messageIds.Add(update.Message.MessageId + 1);
                    await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestNewPassword);
                    return;
                }

                await VerificationCodeService.MarkVerificationCodeAsUsedByUserIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredVerificationCodePasswordWrong);
            }
            else if (_previousMessage.Equals(ButtonTypeToString(ButtonType.ChangePassword)))
            {
                var passwordPattern = @"^[^\s]{8,}$";

                if (!Regex.IsMatch(messageText, passwordPattern))
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredPasswordIncorrectly);
                    return;
                }

                _previousMessage = messageText;
                await UserService.UpdateUserByIdAsync(userId, password: messageText);
                _messageIds.Add(update.Message.MessageId);
                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserChangedPasswordSuccessfully);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.RemoveAccount), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.RemoveAccount);
                _messageIds.Clear();
                _messageIds.Add(update.Message.MessageId + 1);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RequestAccountRemovalConfirmation);
            }
            else if (_previousMessage.Equals(ButtonTypeToString(ButtonType.RemoveAccount)))
            {
                _previousMessage = messageText;

                if (messageText.Equals("ВИДАЛИТИ"))
                {
                    await UserService.RemoveUserByIdAsync(userId);
                    await DeleteOldMessages(botClient, update);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserRemovedAccountSuccessfully);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.PressRegistrationButton);
                    return;
                }

                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserEnteredAccountRemovalConfirmationWrong);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.ChooseSetting);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Contacts), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Contacts);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.Contacts);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Registration), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Registration);
                var user = await UserService.GetUserByIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsAlreadyRegistered);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.Verification), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.Verification);
                var user = await UserService.GetUserByIdAsync(userId);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserIsAlreadyVerified);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.CarSearch), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.CarSearch);

                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);

                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);

                if (carRental != null && carRental.Status == CarRentalStatusType.Processing)
                {
                    await RentalService.RemoveCarRentalByUserIdAsync(userId);
                }

                await PaginationService.SendParkingSpotsAsync(botClient, update);

                var parkingSpots = await RentalService.GetParkingSpotsAsync(CarType.None);

                if (parkingSpots.Count == 0)
                {
                    await botClient.DeleteMessage(update.Message.Chat.Id, update.Message.MessageId);
                }
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.CarRental), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.CarRental);
                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                var user = await UserService.GetUserByIdAsync(userId);

                await DeleteOldMessages(botClient, update);
                _messageIds.Add(update.Message.MessageId + 1);

                if (carRental != null)
                {
                    if (carRental.Status == CarRentalStatusType.Processing)
                    {
                        await RentalService.RemoveCarRentalByUserIdAsync(userId);
                    }
                    else if (carRental.Status == CarRentalStatusType.Confirmed && carRental.RentalStart.AddMinutes(15) < DateTime.Now)
                    {
                        var car = await RentalService.GetCarByIdAsync(carRental.CarId);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalForcedCancellation, car: car);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                        await RentalService.UpdateCarRentalByUserIdAsync(userId, status: CarRentalStatusType.Canceled);
                        await EmailService.SendCarRentalCancellationAsync(user.Email, carRental, true);
                        return;
                    }
                    else if (carRental.Status == CarRentalStatusType.Confirmed)
                    {
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalInformation, carRental: carRental);
                        return;
                    }
                }

                carRental = new CarRental(userId);
                await RentalService.SaveCarRentalAsync(carRental);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.DoYouWantToSelectCarFilter);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.ImOnPlace), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.ImOnPlace);
                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                var user = await UserService.GetUserByIdAsync(userId);
                var admins = await UserService.GetAdminsAsync();

                if (admins == null || admins.Count == 0)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.AdminsEmpty);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                if (carRental == null || carRental.Status != CarRentalStatusType.Confirmed)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalNoActiveRental);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                if (carRental != null)
                {
                    if (carRental.Status == CarRentalStatusType.Confirmed && carRental.RentalStart.AddMinutes(15) < DateTime.Now)
                    {
                        var car = await RentalService.GetCarByIdAsync(carRental.CarId);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalForcedCancellation, car: car);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                        await RentalService.UpdateCarRentalByUserIdAsync(userId, status: CarRentalStatusType.Canceled);
                        await EmailService.SendCarRentalCancellationAsync(user.Email, carRental, true);
                        return;
                    }
                    else if (carRental.Status == CarRentalStatusType.Confirmed && carRental.RentalStart.AddMinutes(-15) > DateTime.Now)
                    {
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalUserCameTooEarly, carRental: carRental);
                        await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                        return;
                    }
                }

                if (carRental.RentalStart > DateTime.Now)
                {
                    await RentalService.UpdateCarRentalByUserIdAsync(userId,
                                                                     rentalEnd: carRental.RentalEnd.AddMinutes(
                                                                        -(carRental.RentalStart - DateTime.Now).TotalMinutes));
                }

                await RentalService.UpdateCarRentalByUserIdAsync(userId, rentalStart: DateTime.Now);
                await VerificationService.UpdateUserLicenseStatusByUserIdAsync(userId, false);

                foreach (var admin in admins)
                {
                    await botClient.SendMessage(
                        chatId: admin.UserId,
                        text: $"ℹ️ Отримано *запит* на *перевірку ВП* від користувача *{user.Nickname}*.",
                        parseMode: ParseMode.Markdown);
                }

                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.WaitForLicenseVerification);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.CancelRent), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.CancelRent);
                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                var user = await UserService.GetUserByIdAsync(userId);

                if (carRental == null || carRental.Status != CarRentalStatusType.Confirmed)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalNoActiveRental);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                var car = await RentalService.GetCarByIdAsync(carRental.CarId);

                await RentalService.UpdateCarRentalByUserIdAsync(userId, status: CarRentalStatusType.Canceled);
                await DeleteOldMessages(botClient, update);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalCancellation, car: car);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                await EmailService.SendCarRentalCancellationAsync(user.Email, carRental);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.UnlockCar), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.UnlockCar);
                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                var car = await RentalService.GetCarByIdAsync(carRental.CarId);
                var user = await UserService.GetUserByIdAsync(userId);

                if (carRental == null || carRental.Status != CarRentalStatusType.Confirmed)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalNoActiveRental);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                var userLicense = await VerificationService.GetUserLicenseByUserIdAsync(userId);

                if (!userLicense.Checked)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalTryingToUnlockWithoutConfirmation);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                    return;
                }

                await RentalService.UpdateCarRentalByUserIdAsync(userId, status: CarRentalStatusType.Active);
                await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalCarUnlockedSuccessfully,
                                                 car: car, carRental: carRental);
                await EmailService.SendCarRentalStartAsync(user.Email, carRental);
            }
            else if (messageText.Contains(ButtonTypeToString(ButtonType.EndRent), StringComparison.OrdinalIgnoreCase))
            {
                _previousMessage = ButtonTypeToString(ButtonType.EndRent);
                var carRental = await RentalService.GetLastCarRentalByUserIdAsync(userId);
                var user = await UserService.GetUserByIdAsync(userId);

                if (carRental != null && carRental.Status == CarRentalStatusType.Active)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalEnd, carRental: carRental);
                    return;
                }

                if (carRental == null || carRental.Status != CarRentalStatusType.Confirmed)
                {
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.RentalNoActiveRental);
                    await Sender.SendBotMessageAsync(botClient, update, BotMessageType.UserGreeting, user: user);
                }
            }
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private static string ButtonTypeToString(ButtonType buttonType)
        {
            return buttonType switch
            {
                ButtonType.None => "None",
                ButtonType.Start => "/start",
                ButtonType.MainMenu => "Головне Меню",
                ButtonType.Settings => "Налаштування",
                ButtonType.ChangeNickname => "Змінити Нікнейм",
                ButtonType.ChangePassword => "Змінити Пароль",
                ButtonType.RemoveAccount => "Видалити Аккаунт",
                ButtonType.Contacts => "Контакти",
                ButtonType.Registration => "Реєстрація",
                ButtonType.Verification => "Верифікація",
                ButtonType.CarSearch => "Пошук Авто",
                ButtonType.CarRental => "Оренда Авто",
                ButtonType.ImOnPlace => "Я біля Авто",
                ButtonType.CancelRent => "Скасувати Оренду",
                ButtonType.UnlockCar => "Розблокувати Авто",
                ButtonType.EndRent => "Завершити Оренду",
                ButtonType.LicenseCheck => "Перевірка ВП",
                ButtonType.VerificationRequests => "Заявки на Верифікацію",
                _ => "None"
            };
        }

        private static async Task DeleteOldMessages(ITelegramBotClient botClient, Update update, long? chatId = null)
        {
            chatId ??= update.Message.Chat.Id;

            try
            {
                if (_messageIds.Count > 0)
                {
                    foreach (var msgId in _messageIds)
                    {
                        await botClient.DeleteMessage(chatId, msgId);
                    }
                    _messageIds.Clear();
                }
            }
            catch (ApiRequestException)
            {
                _messageIds.Clear();
            }
        }
    }
}


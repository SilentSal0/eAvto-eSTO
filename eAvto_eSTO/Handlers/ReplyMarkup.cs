using Telegram.Bot.Types.ReplyMarkups;
using eAvto_eSTO.Enums;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Services;

namespace eAvto_eSTO.Handlers
{
    public static class ReplyMarkup
    {
        public static async Task<IReplyMarkup?> GetReplyMarkup(ReplyMarkupType replyMarkupType, long? userId = null)
        {
            var dateTime = DateTime.Today;
            CarRental? carRental = null;
            List<InlineKeyboardButton[]> inlineButtons = [];

            switch (replyMarkupType)
            {
                case ReplyMarkupType.Registration:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "👤 Реєстрація" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                case ReplyMarkupType.RegistrationYesNo:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("👍 Так", CallbackQueryType.RegistrationStringYes.ToString()),
                            InlineKeyboardButton.WithCallbackData("👎 Ні", CallbackQueryType.RegistrationStringNo.ToString())
                        }
                    });

                case ReplyMarkupType.VerificationCodeResendCancel:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📩 Надіслати", CallbackQueryType.VerificationCodeResend.ToString()),
                            InlineKeyboardButton.WithCallbackData("⛔️ Перервати", CallbackQueryType.VerificationCodeCancel.ToString())
                        }
                    });

                case ReplyMarkupType.Verification:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "👥 Верифікація" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                case ReplyMarkupType.VerificationYesNo:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("👍 Так", CallbackQueryType.VerificationStringYes.ToString()),
                            InlineKeyboardButton.WithCallbackData("👎 Ні", CallbackQueryType.VerificationStringNo.ToString())
                        }
                    });

                case ReplyMarkupType.VerificationConfirmDiscard:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Підвердити", CallbackQueryType.VerificationConfirm.ToString()),
                            InlineKeyboardButton.WithCallbackData("❌ Відхилити", CallbackQueryType.VerificationDiscard.ToString())
                        }
                    });

                case ReplyMarkupType.LicenseConfirmDiscard:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Підвердити", CallbackQueryType.LicenseConfirm.ToString()),
                            InlineKeyboardButton.WithCallbackData("❌ Відхилити", CallbackQueryType.LicenseDiscard.ToString())
                        }
                    });

                case ReplyMarkupType.CarFilterYesNo:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("👍 Так", CallbackQueryType.CarFilterYes.ToString()),
                            InlineKeyboardButton.WithCallbackData("👎 Ні", CallbackQueryType.CarFilterNo.ToString())
                        },
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.MainMenu.ToString()) }
                    });

                case ReplyMarkupType.CarFilterType:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("💰 Економ", CallbackQueryType.CarFilterEconom.ToString()) },
                        new[] { InlineKeyboardButton.WithCallbackData("⚖️ Стандарт", CallbackQueryType.CarFilterStandard.ToString()) },
                        new[] { InlineKeyboardButton.WithCallbackData("💎 Преміум", CallbackQueryType.CarFilterPremium.ToString()) },
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarFilterBack.ToString()) }
                    });

                case ReplyMarkupType.CarRentalStartDate:
                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData($"{dateTime.ToShortDateString()}", $"{CallbackQueryType.CarRentalStartDate}0")]);

                    for (int i = 0; i < 6; i += 2)
                    {
                        inlineButtons.AddRange(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddDays(i + 1).ToShortDateString()}", $"{CallbackQueryType.CarRentalStartDate}{i + 1}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddDays(i + 2).ToShortDateString()}", $"{CallbackQueryType.CarRentalStartDate}{i + 2}")
                            }
                        });
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalStartDateBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.CarRentalStartTimeToday:
                    for (int i = DateTime.Now.Hour + 1; i < 24; i += 3)
                    {
                        var row = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i}")
                        };

                        if (i + 1 < 24)
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 1).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i + 1}"));
                        }

                        if (i + 2 < 24)
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 2).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i + 2}"));
                        }

                        inlineButtons.Add(row.ToArray());
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalStartTimeBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.CarRentalStartTime24:
                    for (int i = 0; i < 24; i += 3)
                    {
                        inlineButtons.AddRange(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 1).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i + 1}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 2).ToShortTimeString()}", $"{CallbackQueryType.CarRentalStartTime}{i + 2}")
                            }
                        });
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalStartTimeBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.CarRentalEndDate:
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync((long)userId);
                    dateTime = carRental.RentalEnd;

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData($"{dateTime.ToShortDateString()}", $"{CallbackQueryType.CarRentalEndDate}0")]);

                    for (int i = 0; i < 6; i += 2)
                    {
                        inlineButtons.AddRange(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddDays(i + 1).ToShortDateString()}", $"{CallbackQueryType.CarRentalEndDate}{i + 1}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddDays(i + 2).ToShortDateString()}", $"{CallbackQueryType.CarRentalEndDate}{i + 2}")
                            }
                        });
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalEndDateBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.CarRentalEndTimeToday:
                    carRental = await RentalService.GetLastCarRentalByUserIdAsync((long)userId);
                    dateTime = carRental.RentalEnd.Date;

                    for (int i = carRental.RentalStart.Hour + 1; i < 24; i += 3)
                    {
                        var row = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i}")
                        };

                        if (i + 1 < 24)
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 1).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i + 1}"));
                        }

                        if (i + 2 < 24)
                        {
                            row.Add(InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 2).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i + 2}"));
                        }

                        inlineButtons.Add(row.ToArray());
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalEndTimeBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.CarRentalEndTime24:
                    for (int i = 0; i < 24; i += 3)
                    {
                        inlineButtons.AddRange(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 1).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i + 1}"),
                                InlineKeyboardButton.WithCallbackData($"{dateTime.AddHours(i + 2).ToShortTimeString()}", $"{CallbackQueryType.CarRentalEndTime}{i + 2}")
                            }
                        });
                    }

                    inlineButtons.Add([InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalEndTimeBack.ToString())]);
                    return new InlineKeyboardMarkup(inlineButtons);

                case ReplyMarkupType.SelectedCarBackMainMenu:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarSelectedCarBack.ToString()) }
                    });

                case ReplyMarkupType.SelectedCarRentBackMainMenu:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🔑 Орендувати", CallbackQueryType.CarSelectedCarRent.ToString()) },
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarSelectedCarBack.ToString()) }
                    });

                case ReplyMarkupType.RentalConfirmBackMenu:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Підтвердити", CallbackQueryType.CarRentalRent.ToString()),
                            InlineKeyboardButton.WithCallbackData("❌ Скасувати", CallbackQueryType.MainMenu.ToString())
                        },
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.CarRentalBack.ToString()) }
                    });

                case ReplyMarkupType.ImOnPlaceMainMenu:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "🅿️ Я біля Авто", "❌ Скасувати Оренду" },
                        new KeyboardButton[] { "🏠 Головне Меню" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                case ReplyMarkupType.UnlockCar:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "💳 Розблокувати Авто" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                case ReplyMarkupType.EndRent:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "🅿️ Завершити Оренду" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                case ReplyMarkupType.EndRentYesNo:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("👍 Так", CallbackQueryType.CarRentalEndYes.ToString()),
                            InlineKeyboardButton.WithCallbackData("👎 Ні", CallbackQueryType.CarRentalEndNo.ToString())
                        }
                    });

                case ReplyMarkupType.Settings:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "👤 Змінити Нікнейм", "🔐 Змінити Пароль" },
                        new KeyboardButton[] { "🚮 Видалити Аккаунт", "🏠 Головне Меню" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                case ReplyMarkupType.SettingsBack:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("↩ Повернутися", CallbackQueryType.SettingsBack.ToString()) }
                    });

                case ReplyMarkupType.VerificationCodeSettingsResendCancel:
                    return new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📩 Надіслати", CallbackQueryType.VerificationCodeResend.ToString()),
                            InlineKeyboardButton.WithCallbackData("⛔️ Перервати", CallbackQueryType.VerificationCodeSettingsCancel.ToString())
                        }
                    });

                case ReplyMarkupType.UserMenu:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "🔍 Пошук Авто", "🔑 Оренда Авто" },
                        new KeyboardButton[] { "⚙️ Налаштування", "☎️ Контакти" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                case ReplyMarkupType.AdminMenu:
                    return new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "🗄 Перевірка ВП" },
                        new KeyboardButton[] { "📒 Заявки на Верифікацію" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                default:
                    return null;
            }
        }
    }
}


using System.Net.Mail;
using System.Net;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Json;

namespace eAvto_eSTO.Services
{
    public static class EmailService
    {
        private static readonly string _password;

        static EmailService()
        {
            var rootPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var filePath = rootPath + @"\Config\config.json";
            var config = JsonReader.ReadJsonAsync<ConfigStructure>(filePath).GetAwaiter().GetResult();
            _password = config.AppPassword ?? throw new InvalidDataException("App Password is null.");
        }

        public static async Task SendVerificationCodeAsync(string email, VerificationCode verificationCode)
        {
            var fromAddress = new MailAddress("eavto.esto@gmail.com", "єАвто - єСТО");
            var toAddress = new MailAddress(email);
            var subject = "Код Підтвердження";
            var body = $"Твій код: <strong>{verificationCode.Code}</strong>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _password)
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await Task.Run(() => smtp.Send(message));
        }

        public static async Task SendCarRentalConfirmationAsync(string email, CarRental carRental)
        {
            var car = await RentalService.GetCarByIdAsync(carRental.CarId);
            var parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
            var fromAddress = new MailAddress("eavto.esto@gmail.com", "єАвто - єСТО");
            var toAddress = new MailAddress(email);
            var subject = "Підтвердження Оренди Авто";
            var body = $"👌 Ти <strong>успішно</strong> забронював <strong>{car.Make} {car.Model} {car.Year} {car.Color}</strong>.<br><br>" +
                $"ℹ️ Інформація про <strong>оренду</strong>:<br>" +
                $"🅿️ <strong>Паркінг-спот</strong>: {parkingSpot.Name}\n" +
                $"📍 <strong>Локація</strong>: {parkingSpot.Location}<br>" +
                $"🚙 <strong>Авто</strong>: {car.Make} {car.Model} {car.Year} {car.Color}<br>" +
                $"⏳ <strong>Дата початку</strong>: {carRental.RentalStart:dd.MM.yyyy HH:mm}<br>" +
                $"⌛️ <strong>Дата кінця</strong>: {carRental.RentalEnd:dd.MM.yyyy HH:mm}<br>" +
                $"💳 <strong>До сплати</strong>: {(int)(carRental.RentalEnd - carRental.RentalStart).TotalHours * car.PricePerHour}₴<br><br>" +
                $"⚠️ <strong>Важливо</strong>: якщо ти <strong>не прибудеш</strong> до авто протягом <strong>15 хвилин</strong> " +
                $"після обраного <strong>часу початку</strong> оренди - її буде <strong>скасовано</strong>.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _password)
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await Task.Run(() => smtp.Send(message));
        }

        public static async Task SendCarRentalCancellationAsync(string email, CarRental carRental, bool forced = false)
        {
            var car = await RentalService.GetCarByIdAsync(carRental.CarId);
            var parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
            var fromAddress = new MailAddress("eavto.esto@gmail.com", "єАвто - єСТО");
            var toAddress = new MailAddress(email);
            var subject = "Скасування Оренди Авто";
            var body = !forced ? $"👌 Ти <strong>успішно</strong> скасував оренду <strong> " +
                $"{car.Make} {car.Model} {car.Year} {car.Color}</strong>."
                : $"😔 Ти <strong>не прибув</strong> до авто вчасно, тому оренду " +
                $"<strong>{car.Make} {car.Model} {car.Year} {car.Color}</strong> скасовано.<br><br>" +
                $"😉 Наступного разу будь більш <strong>відповідальною</strong> людиною.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _password)
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await Task.Run(() => smtp.Send(message));
        }

        public static async Task SendCarRentalStartAsync(string email, CarRental carRental)
        {
            var car = await RentalService.GetCarByIdAsync(carRental.CarId);
            var parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
            var fromAddress = new MailAddress("eavto.esto@gmail.com", "єАвто - єСТО");
            var toAddress = new MailAddress(email);
            var subject = "Початок Оренди Авто";
            var body = $"🔑 Автомобіль <strong>успішно</strong> розблоковано.<br><br>" +
                $"💳 Стягнення за <strong>розблокування</strong>: " +
                $"{Math.Round((int)(carRental.RentalEnd - carRental.RentalStart).TotalHours * car.PricePerHour * 0.2m, 2)}₴<br>" +
                $"🕘 <strong>Час початку</strong> оренди: {carRental.RentalStart:dd.MM.yyyy HH:mm}<br>" +
                $"🕞 <strong>Час кінця</strong> оренди: {carRental.RentalEnd:dd.MM.yyyy HH:mm}<br><br>" +
                $"⚠️ <strong>Важливо</strong>: наполегливо просимо <strong>повернути</strong> авто на паркінг-спот вчасно.<br>" +
                $"ℹ️ У випадку, якщо авто <strong>не буде повернено</strong> на паркінг-спот до " +
                $"<strong>{carRental.RentalEnd.AddMinutes(15):dd.MM.yyyy HH:mm}</strong>, " +
                $"його буде автоматично <strong>заблоковано</strong>, а ти <strong>отримаєш</strong> штраф.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _password)
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await Task.Run(() => smtp.Send(message));
        }

        public static async Task SendCarRentalEndAsync(string email, CarRental carRental)
        {
            var car = await RentalService.GetCarByIdAsync(carRental.CarId);
            var parkingSpot = await RentalService.GetParkingSpotByIdAsync(carRental.SpotId);
            var fromAddress = new MailAddress("eavto.esto@gmail.com", "єАвто - єСТО");
            var toAddress = new MailAddress(email);
            var subject = "Завершення Оренди Авто";
            var body = $"👌 Ти <strong>успішно</strong> завершив оренду " +
                $"<strong>{car.Make} {car.Model} {car.Year} {car.Color}</strong>.<br><br>" +
                $"💳 Стягнення за <strong>оренду</strong>: {Math.Round((int)(carRental.RentalEnd - carRental.RentalStart).TotalHours
                * car.PricePerHour * 0.8m, 2)}₴<br><br>" +
                $"⚠️ <strong>Важливо</strong>: <strong>покинь</strong> авто протягом <strong>15 хвилин</strong>, " +
                $"щоб <strong>уникнути</strong> проблем.<br>" +
                $"ℹ️ Як тільки ти <strong>покинеш</strong> авто, ти більше " +
                $"<strong>не зможеш</strong> відчинити його <strong>двері</strong>.";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, _password)
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await Task.Run(() => smtp.Send(message));
        }
    }
}


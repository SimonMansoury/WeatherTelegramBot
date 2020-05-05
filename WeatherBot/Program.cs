using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.ClassesforWeatherInfo;
using WeatherBot.PictureClasses;

class Program
{
    private static TelegramBotClient Bot;

    static string WeatherAPIKEYfile = "WeatherApiKey.txt";
    static string pathForWeather = Path.Combine(Environment.CurrentDirectory, @"keys\", WeatherAPIKEYfile);
    static string PicturesAPIKEYfile = "PicturesApiKey.txt";
    static string pathForPictures = Path.Combine(Environment.CurrentDirectory, @"keys\", PicturesAPIKEYfile);

    static string tokenFile = "token.txt";
    static string pathForToken = Path.Combine(Environment.CurrentDirectory, @"keys\", tokenFile);

    static string WeatherAPIKEY = System.IO.File.ReadAllText(pathForWeather);
    static string PicturesAPIKEY = System.IO.File.ReadAllText(pathForPictures);
    static string token = System.IO.File.ReadAllText(pathForToken);

    static void Main(string[] args)
    {
        Bot = new TelegramBotClient(token);
        Bot.OnMessage += BotOnMessageReceived;
        Bot.OnMessageEdited += BotOnMessageReceived;
        Bot.OnCallbackQuery += BotOnButtonClick;

        Bot.StartReceiving();

        Console.ReadLine();
        Bot.StopReceiving();
    }
    static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
    {
        var message = messageEventArgs.Message;

        if (message == null || message.Type != MessageType.Text)
            return;

        switch (message.Text)
        {
            case "/start":
                Console.WriteLine($"User @{messageEventArgs.Message.From.Username} was connected to bot");
                await StartMessage(message);
                break;

            case "/help":
                await HelpMessage(message);
                break;
            default:

                var info = GetWeatherInfoAboutCity(message.Text);

                if (info == null)
                {
                    await Bot.SendTextMessageAsync
                       (
                       message.Chat.Id,
                       "Не могу найти погоду по Вашему городу😞",
                       disableNotification: false
                       );
                }
                else
                {
                    string[] Phrases = { "Секундочку, получаю данные...⏳", "Получаю информацию...📡", "Спрашиваю у синоптика...📲",
                    "Высылаю ответ...🌚"};
                    Random rnd = new Random();
                    try
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, Phrases[rnd.Next(0, Phrases.Count())], disableNotification: false);
                        await SendMessageWithWeatherInfo(message.Text, message.Chat.Id, info);
                        await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId + 1);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
                        Console.WriteLine(new string('-', 140));
                    }
                }

                break;
        }
    }
    static async void BotOnButtonClick(object sc, CallbackQueryEventArgs ev)
    {
        try
        {
            var message = ev.CallbackQuery.Message;
            var city = ev.CallbackQuery.Data.Split('_').First();

            if (ev.CallbackQuery.Data.Contains("forecast"))
            {
                int daysNum = int.Parse(ev.CallbackQuery.Data.Last().ToString());

                await SendMessageWithWeatherInfo(message, GetWeatherInfoAboutCity(city, daysNum));
            }
            else if (ev.CallbackQuery.Data.Contains("_more"))
            {
                await SendMessageWithWeatherInfo(city, message.Chat.Id, GetWeatherInfoAboutCity(city), more: true, MessageId: ev.CallbackQuery.Message.MessageId);
            }
            else if (ev.CallbackQuery.Data.Contains("_tomorrow"))
            {
                await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                var days = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Прогноз на завтра", $"{city}_forecast1"),
                        InlineKeyboardButton.WithCallbackData("Прогноз на 3 дня", $"{city}_forecast3")
                    }
                });

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "На сколько дней прислать прогноз? 🤔",
                    disableNotification: true,
                    replyMarkup: days
                    );
            }
            else
            {
                Console.WriteLine(ev.CallbackQuery.Data);
                Console.WriteLine("ERROR! Strange Callback Data");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
        }
    }
    static WeatherForecast GetWeatherInfoAboutCity(string city, int days)
    {
        try
        {
            var url = $"http://api.weatherapi.com/v1/forecast.json?key={WeatherAPIKEY}&q={city}&days={days}&lang=ru";

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string response;
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            WeatherForecast Forecast = JsonConvert.DeserializeObject<WeatherForecast>(response);
            return Forecast;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
            return null;
        }

    }
    static Weather GetWeatherInfoAboutCity(string city)
    {
        try
        {
            string response;
            var url = $"http://api.weatherapi.com/v1/current.json?key={WeatherAPIKEY}&q={city}&lang=ru";

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            Weather weatherInfo = JsonConvert.DeserializeObject<Weather>(response);
            return weatherInfo;
        }
        catch(WebException)
        {
            Console.WriteLine($"Couldn`t find city - {city}");
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType() + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
            return null;
        }
    }
    static string GetPicturesByTopic(string topic, bool findMoreGlobal = true)
    {
        try
        {
            string url = $"https://pixabay.com/api/?key={PicturesAPIKEY}&q={topic}+город&image_type=photo&orientation=horizontal&category=places&pretty=true";

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string response;
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            PicturesByTopic picturesByTopic = JsonConvert.DeserializeObject<PicturesByTopic>(response);

            Random rnd = new Random();
            return (picturesByTopic.hits.Count > 0) ? picturesByTopic.hits[rnd.Next(0, picturesByTopic.hits.Count)].webformatURL : picturesByTopic.hits.First().webformatURL;

        }
        catch(InvalidOperationException)
        {
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
            return null;
        }

    }

    static async Task SendMessageWithWeatherInfo(Message message, WeatherForecast WeatherForecast)
    {
        try
        {
            await Bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);

            var ChatId = message.Chat.Id;
            var BotMessage = new StringBuilder($"Прогноз в городе {WeatherForecast.location.name}, {WeatherForecast.location.country}\n");
            BotMessage.Append(new string('-', 70) + "\n");

            foreach (var forecast in WeatherForecast.forecast.forecastday)
            {
                BotMessage.Append($"{forecast.date}: " +
                    $"\t{forecast.day.condition.text}\n" +
                    $"\tТемпература {(int)forecast.day.avgtemp_c}°\n" +
                    $"\tМаксимальная {(int)forecast.day.maxtemp_c}°\n" +
                    $"\tШанс выпадения осадков {forecast.day.daily_chance_of_rain}%\n" + new string('-', 70) + "\n");
            }

            await Bot.SendTextMessageAsync(ChatId, BotMessage.ToString(), disableNotification: true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
        }
    }
    static async Task SendMessageWithWeatherInfo(string message, long ChatId, Weather Weather, bool more = false, int MessageId = 0)
    {
        string BotMessage = "";
        var pictureURL = GetPicturesByTopic(message);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // first row
            {
                InlineKeyboardButton.WithCallbackData("Больше информации о погоде", $"{message}_more")
            },
            new [] // second row
            {
                InlineKeyboardButton.WithCallbackData("Прогноз погоды", $"{message}_tomorrow")
            }
        });

        try
        {
            if (!more)
            {
                BotMessage = $"Погода в городе: {Weather.location.name}, {Weather.location.country}" +
                $"\nСейчас {Weather.current.condition.text}" +
                $"\nТемпература {(int)Weather.current.temp_c}°" +
                $"\nОщущается как {(int)Weather.current.feelslike_c}°";

                if (pictureURL != null)
                {
                    await Bot.SendPhotoAsync
                       (
                       ChatId,
                       photo: pictureURL,
                       caption: BotMessage,
                       disableNotification: false,
                       replyMarkup: keyboard
                       );
                }
                else if (pictureURL == null)
                {
                    await Bot.SendTextMessageAsync
                        (
                        chatId: ChatId,
                        text: BotMessage,
                        disableNotification: false,
                        replyMarkup: keyboard
                        );
                }
            }
            else
            {
                var keyboardWithMoreInfo = new InlineKeyboardMarkup(new[]
                {
                    new [] // second row
                    {
                        InlineKeyboardButton.WithCallbackData("Прогноз погоды", $"{message}_tomorrow")
                    }
                });
                BotMessage = $"Погода в городе: {Weather.location.name}, {Weather.location.country}" +
                $"\nСейчас {Weather.current.condition.text}" +
                $"\nТемпература {(int)Weather.current.temp_c}°" +
                $"\nОщущается как {(int)Weather.current.feelslike_c}°" +
                $"\nВидимость {Weather.current.vis_km} км" +
                $"\nДавление {Weather.current.pressure_mb} мм ртут. столб." +
                $"\nВлажность {Weather.current.humidity}%" +
                $"\nСкорость ветра {(int)(Weather.current.wind_kph / 3.6)} м/с" +
                $"\nНаправление {WindDirection(Weather.current.wind_degree)}" +
                $"\nОблачность: {Weather.current.cloud}%";

                await Bot.DeleteMessageAsync(ChatId, MessageId);

                if (pictureURL != null)
                {
                    await Bot.SendPhotoAsync
                        (
                        ChatId,
                        photo: pictureURL,
                        caption: BotMessage,
                        disableNotification: false,
                        replyMarkup: keyboardWithMoreInfo
                        );
                }
                else if(pictureURL== null)
                {
                    await Bot.SendTextMessageAsync
                        (
                        chatId: ChatId,
                        text: BotMessage,
                        disableNotification: false,
                        replyMarkup: keyboardWithMoreInfo
                        );
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
        }
    }
    static async Task StartMessage(Message message)
    {
        var ChatId = message.Chat.Id;
        await Bot.SendTextMessageAsync(ChatId, "Привет! Этот бот поможет тебе узнавать погоду ☀️💦❄️🌤 в твоем городе, для этого" +
           " пришли мне название города, погода в котором, тебя интересует");
    }
    static async Task HelpMessage(Message message)
    {
        var ChatId = message.Chat.Id;
        await Bot.SendTextMessageAsync(ChatId, "Помощь");
    }
    static string WindDirection(int deg)
    {
        if (deg == 0 && deg == 360)
            return "Северный";
        else if (deg > 0 && deg < 90)
            return "Северо-Восточный";
        else if (deg == 90)
            return "Восточный";
        else if (deg > 90 && deg < 180)
            return "Юго-Восточный";
        else if (deg == 180)
            return "Южный";
        else if (deg > 180 && deg < 270)
            return "Юго-Западный";
        else if (deg == 270)
            return "Западный";
        else if (deg > 270 && deg < 360)
            return "Северо-Западный";
        else
            return "";

    }

}
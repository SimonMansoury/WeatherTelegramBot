using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using WeatherBot;
using WeatherBot.ClassesforWeatherInfo.ClassesForForecast;
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


    static async void BotOnButtonClick(object sc, CallbackQueryEventArgs ev)
    {
        try
        {
            var message = ev.CallbackQuery.Message;
            
            if (ev.CallbackQuery.Data.Contains("more"))
            {
                var city = ev.CallbackQuery.Data.Split('_').First();
                await SendMessageWithWeatherInfo(city, message.Chat.Id, GetWeatherInfoAboutCity(city), more: true, MessageId: ev.CallbackQuery.Message.MessageId);
            }
            else if (ev.CallbackQuery.Data.Contains("toworrow"))
            {
                var city = ev.CallbackQuery.Data.Split('_').First();
                await SendMessageWithWeatherInfo(message, GetWeatherInfoAboutCity(message, 1));
            }
            else
            {
                Console.WriteLine("ERROR!");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
        }
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
    static Forecast GetWeatherInfoAboutCity(Message message, int days)
    {
        try
        {
            var url = $"http://api.openweathermap.org/data/2.5/forecast/daily?q={message.Text}&cnt={days}&units=metric&lang=ru&appid={WeatherAPIKEY}";

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string response;
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            Forecast Forecast = JsonConvert.DeserializeObject<Forecast>(response);
            return Forecast;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 140));
            return null;
        }

    }
    static WeatherInfo GetWeatherInfoAboutCity(string city)
    {

        try
        {
            string response;
            var url = $"http://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&lang=ru&appid={WeatherAPIKEY}";

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            WeatherInfo weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(response);
            return weatherInfo;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
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
    static async Task SendMessageWithWeatherInfo(Message message, Forecast WeatherForecast)
    {
        var ChatId = message.Chat.Id;
        var BotMessage = $"Прогноз в городе {WeatherForecast.city}";
        foreach (var dateTime in WeatherForecast.list)
        {
            Console.WriteLine(dateTime.dt);
        }

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

    static async Task SendMessageWithWeatherInfo(string message, long ChatId, WeatherInfo WeatherInfo, bool more = false, int MessageId = 0)
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
                InlineKeyboardButton.WithCallbackData("Прогноз на завтра", $"{message}_tomorrow")
            }
        });

        try
        {
            if (!more)
            {

                BotMessage = $"Погода в городе: {WeatherInfo.name}" +
                $"\nСейчас {WeatherInfo.weather[0].description}" +
                $"\nТемпература {(int)WeatherInfo.main.temp}°" +
                $"\nМаксимальная {(int)WeatherInfo.main.temp_max}°" +
                $"\nОщущается как {(int)WeatherInfo.main.feels_like}°";

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
                        InlineKeyboardButton.WithCallbackData("Прогноз на завтра", $"{message}_tomorrow")
                    }
                });
                BotMessage = $"Погода в городе: {WeatherInfo.name}" +
                $"\nСейчас {WeatherInfo.weather[0].description}" +
                $"\nТемпература {(int)WeatherInfo.main.temp}°" +
                $"\nМаксимальная {(int)WeatherInfo.main.temp_max}°" +
                $"\nОщущается как {(int)WeatherInfo.main.feels_like}°" +
                $"\nВидимость {WeatherInfo.visibility / 1000} км" +
                $"\nДавление {WeatherInfo.main.pressure} мм ртут. столб." +
                $"\nВлажность {WeatherInfo.main.humidity}%" +
                $"\nСкорость ветра {WeatherInfo.wind.speed} м/с" +
                $"\nНаправление {WindDirection(WeatherInfo.wind.deg)}" +
                $"\nОблачность: {WeatherInfo.clouds.all}%";

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
}
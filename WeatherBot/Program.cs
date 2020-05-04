using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

    static string WeatherAPIKEY = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\WeatherApiKey.txt");
    static string PicturesAPIKEY = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\PicturesApiKey.txt");
    static string token = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\token.txt");

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
                await SendMessageWithWeatherInfo(message, GetWeatherInfoAboutCity(city), more: true, MessageId: ev.CallbackQuery.Message.MessageId);
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
            Console.WriteLine(new string('-', 70));
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
                    await SendMessageWithWeatherInfo(message, info);
               
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
    static  Forecast GetWeatherInfoAboutCity(Message message, int days)
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
            Console.WriteLine(new string('-', 70));
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
        catch(Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 70));
            return null;
        }
    }
    static string GetPicturesByTopic(Message message, bool findMoreGlobal = true)
    {
        try
        {
            var topic = message.Text;
            string url = $"https://pixabay.com/api/?key={PicturesAPIKEY}&q={topic}&image_type=photo&orientation=horizontal&category=places&pretty=true";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string response;
            using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                response = stream.ReadToEnd();
            }
            PicturesByTopic picturesByTopic = JsonConvert.DeserializeObject<PicturesByTopic>(response);

            Random rnd = new Random();
            return (picturesByTopic.hits.Count > 0) ?  picturesByTopic.hits[rnd.Next(0, picturesByTopic.hits.Count)].webformatURL : picturesByTopic.hits.First().webformatURL;
             
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n" + e.Source + "\n" + e.StackTrace);
            Console.WriteLine(new string('-', 70));
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

    static async Task SendMessageWithWeatherInfo(Message message, WeatherInfo WeatherInfo, bool more = false, int MessageId = 0)
	{
		var ChatId = message.Chat.Id;
        string BotMessage = "";
        var pictureURL = GetPicturesByTopic(message);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // first row
            {
                InlineKeyboardButton.WithCallbackData("Больше информации о погоде", $"{message.Text}_more")
            },
            new [] // second row
            {
                InlineKeyboardButton.WithCallbackData("Прогноз на завтра", $"{message.Text}_tomorrow")
            }
        });

        try
        {
            if(!more)
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
                else
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
                BotMessage = $"Погода в городе: {WeatherInfo.name}" +
                $"\nСейчас {WeatherInfo.weather[0].description}" +
                $"\nТемпература {(int)WeatherInfo.main.temp}°" +
                $"\nМаксимальная {(int)WeatherInfo.main.temp_max}°" +
                $"\nОщущается как {(int)WeatherInfo.main.feels_like}°" +
                $"\nВидимость {WeatherInfo.visibility}м" +
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
                        replyMarkup: keyboard
                        );
                }
                else
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

        }
        catch (Exception e)
		{
			Console.WriteLine(e.Message+"\n"+e.Source + "\n"+e.StackTrace);
            Console.WriteLine(new string('-', 70));
        }
    }
}
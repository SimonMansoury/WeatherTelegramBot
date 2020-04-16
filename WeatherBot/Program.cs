using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot;
using WeatherBot.PictureClasses;

class Program
{
	private static TelegramBotClient client;
    static string WeatherAPIKEY = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\WeatherApiKey.txt");
    static string PicturesAPIKEY = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\PicturesApiKey.txt");
    static string token = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\token.txt");

    static void Main(string[] args)
	{
        client = new TelegramBotClient(token);
		client.OnMessage += BotOnMessageReceived;
		client.OnMessageEdited += BotOnMessageReceived;
		client.StartReceiving();

		Console.ReadLine();
		client.StopReceiving();
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
                try
                {
                    await FindWeather(message);
                }
                catch(WebException)
                {
                    await client.SendTextMessageAsync
                        (
                        message.Chat.Id,
                        "Не могу найти погоду по Вашему городу😞",
                        disableNotification: false
                        );
                }
                break;
		}
	}
	static async Task StartMessage(Message message)
	{
		var ChatId = message.Chat.Id;
		await client.SendTextMessageAsync(ChatId, "Привет! Этот бот поможет тебе узнавать погоду ☀️💦❄️🌤 в твоем городе, для этого" +
			" пришли мне название города, погода в котором, тебя интересует");

	}
	static async Task HelpMessage(Message message)
	{
		var ChatId = message.Chat.Id;
		await client.SendTextMessageAsync(ChatId, "Помощь");
	}
    static WeatherInfo GetWeatherInfoMessage(Message message)
    {
        var url = $"http://api.openweathermap.org/data/2.5/weather?q={message.Text}&units=metric&lang=ru&appid={WeatherAPIKEY}";
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

        string response;
        using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
        {
            response = stream.ReadToEnd();
        }
        WeatherInfo weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(response);
        return weatherInfo;
    }
    static string GetPicturesByTopic(Message message)
    {
        var topic = message.Text;
        var url = $"https://pixabay.com/api/?key={PicturesAPIKEY}&q={topic}&image_type=photo&orientation=horizontal&category=places&pretty=true";
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

        string response;
        using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
        {
            response = stream.ReadToEnd();
        }
        PicturesByTopic picturesByTopic = JsonConvert.DeserializeObject<PicturesByTopic>(response);
        return picturesByTopic.hits.First().webformatURL;
    }

    static async Task FindWeather(Message message)
	{
		var ChatId = message.Chat.Id;
        var weatherInfo = GetWeatherInfoMessage(message);

        var BotMessage = $"Погода в городе: {weatherInfo.name}" +
            $"\nСейчас {weatherInfo.weather[0].description}" +
            $"\nТемпература {(int)weatherInfo.main.temp}°" +
            $"\nМаксимальная {(int)weatherInfo.main.temp_max}°" +
            $"\nОщущается как {(int)weatherInfo.main.feels_like}°";

        try
		{
            var pictureURL = GetPicturesByTopic(message);
            await client.SendPhotoAsync(
                ChatId,
                photo: pictureURL,
                caption: BotMessage,
                disableNotification: false
                );

        }
        catch(InvalidOperationException)
        {
            await client.SendTextMessageAsync
                        (
                        ChatId,
                        BotMessage,
                        disableNotification: false
            );
        }
        catch (Exception e)
		{
			Console.WriteLine(e.Message+"\n"+e.Source + "\n"+e.StackTrace);
		}
	}
}
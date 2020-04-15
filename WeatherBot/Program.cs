using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

class Program
{
	private static TelegramBotClient client;
    static string APIKEY = System.IO.File.ReadAllText(@"D:\WeatherBot\SensitiveInfo\weatherapikey.txt");
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
				await StartMessage(message);
				break;

			case "/help":
				await HelpMessage(message);
				break;

			default:
				await FindWeather(message);
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

	static async Task FindWeather(Message message)
	{
		var ChatId = message.Chat.Id;
		try
		{
            var url = $"http://api.openweathermap.org/data/2.5/weather?q={message.Text}&units=metric&lang=ru&appid={APIKEY}";
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

			string response;
			using (StreamReader stream = new StreamReader(httpWebResponse.GetResponseStream()))
			{
				response = stream.ReadToEnd();
			}
			WeatherInfo weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(response);

            var BotMessage = $"Погода в городе: {weatherInfo.name}" +
                $"\nСейчас {weatherInfo.weather[0].description}" +
                $"\nТемпература {weatherInfo.main.temp}°" +
                $"\nМаксимальная {weatherInfo.main.temp_max}°" +
                $"\nОщущается как {weatherInfo.main.feels_like}°";


            await client.SendTextMessageAsync
                (
                ChatId,
                BotMessage,
                disableNotification: false
				);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
			await client.SendTextMessageAsync(ChatId, $"{e.Message}");
		}
	}
}
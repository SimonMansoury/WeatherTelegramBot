using System;
using System.Collections.Generic;
using System.Text;

namespace WeatherBot.ClassesforWeatherInfo.ClassesForForecast
{
    public class Forecast
    {
        public string cod { get; set; }
        public int message { get; set; }
        public City city { get; set; }
        public int cnt { get; set; }
        public List<List> list { get; set; }
    }
}

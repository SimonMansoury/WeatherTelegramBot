using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WeatherBot.ClassesforWeatherInfo
{
    public class CurrentForForecast
    {
        public double temp_c { get; set; }
        public Condition condition { get; set; }
        public double wind_kph { get; set; }
        public int wind_degree { get; set; }
        public string wind_dir { get; set; }
        public double pressure_mb { get; set; }
        public int humidity { get; set; }
        public int cloud { get; set; }
        public double feelslike_c { get; set; }
        public double vis_km { get; set; }
        public double uv { get; set; }
        public double gust_kph { get; set; }
    }
    public class Day
    {
        public double maxtemp_c { get; set; }
        public double mintemp_c { get; set; }
        public double avgtemp_c { get; set; }
        public double maxwind_kph { get; set; }
        public double totalprecip_mm { get; set; }
        public double avgvis_km { get; set; }
        public double avgvis_miles { get; set; }
        public double avghumidity { get; set; }
        public int daily_will_it_rain { get; set; }
        public string daily_chance_of_rain { get; set; }
        public int daily_will_it_snow { get; set; }
        public string daily_chance_of_snow { get; set; }
        public Condition condition { get; set; }
        public double uv { get; set; }
    }

    public class Astro
    {
        public string sunrise { get; set; }
        public string sunset { get; set; }
    }

    public class Forecastday
    {
        public string date { get; set; }
        public Day day { get; set; }
        public Astro astro { get; set; }
    }

    public class Forecast
    {
        public IList<Forecastday> forecastday { get; set; }
    }

    public class Alert
    {
    }

    public class WeatherForecast
    {
        public Location location { get; set; }

        [JsonProperty("Current")]
        public CurrentForForecast current { get; set; }
        public Forecast forecast { get; set; }
        public Alert alert { get; set; }
    }
}


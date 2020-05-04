using System;
using System.Collections.Generic;
using System.Text;

namespace WeatherBot.ClassesforWeatherInfo.ClassesForForecast
{
    public class City
    {
        public int geoname_id { get; set; }
        public string name { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public string country { get; set; }
        public string iso2 { get; set; }
        public string type { get; set; }
        public int population { get; set; }
    }

   

    
}

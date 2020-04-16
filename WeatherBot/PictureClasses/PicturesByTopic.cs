using System;
using System.Collections.Generic;
using System.Text;

namespace WeatherBot.PictureClasses
{
    public class PicturesByTopic
    {
        public int total { get; set; }
        public int totalHits { get; set; }
        public List<Picture> hits { get; set; }
        public PicturesByTopic()
        {
            hits = new List<Picture>();
        }
    }
}

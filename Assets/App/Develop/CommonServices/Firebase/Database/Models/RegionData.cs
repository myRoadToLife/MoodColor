using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class RegionData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("center")]
        public GeoPoint Center { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("emotions")]
        public Dictionary<string, int> Emotions { get; set; } = new Dictionary<string, int>();
    }

    [Serializable]
    public class GeoPoint
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        public GeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}

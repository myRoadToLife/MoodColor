using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Models
{
    [Serializable]
    public class RegionData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public GeoLocation Location { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("emotions")]
        public Dictionary<string, int> Emotions { get; set; } = new Dictionary<string, int>();
    }

    [Serializable]
    public class GeoLocation
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        public GeoLocation()
        {
        }

        public GeoLocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}

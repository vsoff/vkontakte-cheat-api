using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkCheatApiLibrary.Models
{
    public class Photo
    {
        [JsonProperty("date")]
        private long _date { get; set; }

        [JsonIgnore]
        public DateTime Date
        {
            get { return DateTimeOffset.FromUnixTimeSeconds(_date).UtcDateTime; }
            set { this._date = (int)(value - new DateTime(1970, 1, 1)).TotalSeconds; }
        }
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("album_id")]
        public int AlbumId { get; set; }
        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("access_key")]
        public string AccessKey { get; set; }
        [JsonProperty("sizes")]
        public PhotoSizes[] Sizes { get; set; }
    }

    public class PhotoSizes
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
    }
}

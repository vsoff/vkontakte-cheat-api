using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkCheatApiLibrary.Models
{
    public class Attachment
    {
        [JsonProperty("photo")]
        public Photo Photo { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}

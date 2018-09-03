using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkCheatApiLibrary.Models
{
    public class HistoryAttachmentsObject
    {
        [JsonProperty("response")]
        public HistoryAttachmentsResponse Response { get; set; }
    }

    public class HistoryAttachmentsResponse
    {
        [JsonProperty("items")]
        public HistoryAttachmentsItem[] Items { get; set; }
        [JsonProperty("next_from")]
        public string NextFrom { get; set; }
    }

    public class HistoryAttachmentsItem
    {
        [JsonProperty("attachment")]
        public Attachment Attachment { get; set; }
        [JsonProperty("message_id")]
        public long MessageId { get; set; }
    }
}

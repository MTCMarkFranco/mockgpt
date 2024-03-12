// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
using System.Text.Json.Serialization;

public class PayLoad
    {
        [JsonPropertyName("locale")]
        public string locale { get; set; }

        [JsonPropertyName("session_id")]
        public string session_id { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("history")]
        public List<object> history { get; set; }
    }

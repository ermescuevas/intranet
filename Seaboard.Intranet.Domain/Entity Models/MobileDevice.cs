using Newtonsoft.Json;

namespace Seaboard.Intranet.Domain
{
    public class MobileDevice
    {
        [JsonProperty("DeviceName")]
        public string DeviceName { get; set; }
        [JsonProperty("Brand")]
        public string Brand { get; set; }
        [JsonProperty("technology")]
        public string Technology { get; set; }
    }
}

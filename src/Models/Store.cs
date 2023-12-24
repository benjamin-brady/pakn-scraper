namespace Scraper.Models;
using Newtonsoft.Json;
// a store has an id and a name
/**
                        "id": "3bfe040b-f0c9-45e0-949b-b9ad5a591d55",
   "name": "PAK'nSAVE Papamoa",
   "banner": "PNS",
   "address": "42 Domain Road, Papamoa Beach, Papamoa, 3118",
   "delivery": false,
   "clickAndCollect": true,
   "latitude": -37.70191,
   "longitude": 176.283687,
   */
// create a store which can deserialize the json
public class Store {
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("banner")]
    public string Banner { get; set; }
    [JsonProperty("address")]
    public string Address { get; set; }
    [JsonProperty("delivery")]
    public bool Delivery { get; set; }
    [JsonProperty("clickAndCollect")]
    public bool ClickAndCollect { get; set; }
    [JsonProperty("latitude")]
    public float Latitude { get; set; }
    [JsonProperty("longitude")]
    public float Longitude { get; set; }

    public Store() {
        this.Id = "";
        this.Name = "";
        this.Banner = "";
        this.Address = "";
        this.Delivery = false;
        this.ClickAndCollect = false;
        this.Latitude = 0;
        this.Longitude = 0;
    }
}
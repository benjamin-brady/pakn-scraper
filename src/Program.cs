using System.Diagnostics;
using System.Security.Policy;
using Microsoft.Playwright;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Scraper.Models;
using static Scraper.ProductStore;
using static Scraper.PriceHistoryStore;
using static Scraper.Utilities;

// Pak Scraper
// Scrapes product info and pricing from Pak n Save NZ's website.

namespace Scraper {
    public class Program {
        static int secondsDelayBetweenPageScrapes = 11;
        static bool onlyUploadImagesForNewProducts = false;
        static Stopwatch stopwatch = new Stopwatch();
        
        // Singletons for Playwright
        public static IPlaywright? playwright;
        public static IBrowser? browser;
        public static HttpClient httpclient = new HttpClient();

        // Get config from appsettings.json
        public static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        public static async Task Main(string[] args) {
            Console.WriteLine("Pak Scraper - Scrapes product info and pricing from Pak n Save NZ's website.");
            // Handle arguments - 'dotnet run dry' will run in dry mode, bypassing CosmosDB
            //  'dotnet run reverse' will reverse the order that each page is loaded
            if (args.Length > 0) {
                if (args.Contains("dry")) dryRunMode = true;
                if (args.Contains("reverse")) reverseMode = true;
                Log(ConsoleColor.Yellow, $"\n(Dry Run mode on)");
            }

            // Establish Playwright browser
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await EstablishPlaywright();
            if(playwright == null || browser == null) return;

            // Connect to CosmosDB - end program if unable to connect
            if (!dryRunMode) {
                if (!await ProductStore.EstablishConnection(
                        db: "supermarket-prices",
                        partitionKey: "/name",
                        container: "products"
                    )) return;

                if (!await PriceHistoryStore.EstablishConnection(
                        db: "supermarket-prices",
                        partitionKey: "/productId",
                        container: "prices"
                    )) return;
            }

            // Get all stores from the paknsave site
            List<Store> stores = await GetStores();

            // read categories.json file
            var categoriesJson = File.ReadAllText("categories.json");
            var categories = Newtonsoft.Json.Linq.JObject.Parse(categoriesJson);
            // get allMenuItems
            var allMenuItems = categories["pageProps"]["allMenuItems"] as Newtonsoft.Json.Linq.JArray;
            // get the url property of each item - no need to recurse into children because the top level category contains all products from sub-categories
            var baseUrl = "https://www.paknsave.co.nz";
            var topLevelCategories = allMenuItems?.Select(item => new Category()
                { Url = baseUrl + item["url"], Name = item["name"].ToString() }).ToList();

            // for each url visit it and click .fs-pagination__btn--next until the net button is hidden on the last page
            Log(ConsoleColor.Yellow, $"Found {topLevelCategories.Count} top level categories");

            // Optionally reverse the order of urls
            if (reverseMode) topLevelCategories.Reverse();

            // for each store create a new scraper
            foreach (Store store in stores) {
                var scraper = new Scraper(playwright, browser, store, topLevelCategories);
                await scraper.Scrape();
                // TODO something is not waiting for each scraper to pause
                await Task.Delay(11000);
            }
        }
        
        record StoresResponse(List<Store> stores);

        private static async Task<List<Store>> GetStores() {
            const string storesUrl = "https://www.paknsave.co.nz/CommonApi/Store/GetStoreList";
            var httpclient = new HttpClient();
            httpclient.DefaultRequestHeaders.Add("Accept", "application/json");
            var requestProductDetails = await httpclient.GetAsync(storesUrl);
            var storesJson = await requestProductDetails.Content.ReadAsStringAsync();
            // from the stores property of the json deserialize to a list of stores
            var storesResponse = JsonConvert.DeserializeObject<StoresResponse>(storesJson);
            return storesResponse.stores;
        }


        public async static Task EstablishPlaywright() {
            try {
                // Launch Playwright Browser - Headless mode doesn't work with the anti-bot mechanisms,
                //  so a regular browser window is launched
                playwright = await Playwright.CreateAsync();

                browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions { Headless = false, }
                );
                
                return;
            }
            catch (Microsoft.Playwright.PlaywrightException) {
                Log(
                    ConsoleColor.Red,
                    "Browser must be manually installed using: \n" +
                    "pwsh bin/Debug/net6.0/playwright.ps1 install\n"
                );
                throw;
            }
        }

        // Get the hi-res image url from the Playwright element
        public async static Task<string> GetHiresImageUrl(IElementHandle productElement) {
            // Image URL
            var aTag = await productElement.QuerySelectorAsync("a");
            var imgDiv = await aTag!.QuerySelectorAsync("div div");
            string? imgUrl = await imgDiv!.GetAttributeAsync("data-src-s");

            // Check if image is a valid product image
            if (!imgUrl!.Contains("200x200")) return "";

            // Swap url params to get hi-res version
            return imgUrl = imgUrl!.Replace("200x200", "master");
        }


        private static bool dryRunMode = false;
        private static bool reverseMode = false;
    }
}
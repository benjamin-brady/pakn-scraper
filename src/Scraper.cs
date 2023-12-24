using System.Diagnostics;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Scraper.Models;
using static Scraper.Utilities;

namespace Scraper;

public class Scraper {
    public Stopwatch stopwatch = new();

    static int secondsDelayBetweenPageScrapes = 11;

    public IPlaywright playwright;
    public IBrowser browser;
    public HttpClient httpclient = new HttpClient();
    public IPage? playwrightPage;

    public Scraper(IPlaywright playwright, IBrowser browser, Store store, List<Category> categories) {
        this.store = store;
        this.browser = browser;
        this.categories = categories;
        this.playwright = playwright;
    }

    // The supermarket e.g. Pak'nSave Moorehouse
    public Store store { get; set; }

    public List<Category> categories { get; set; }

    public async Task Scrape() {
        stopwatch.Start();

        playwrightPage = await browser.NewPageAsync();
        // Open a page and allow the geolocation detection system to set the desired location
        await RoutePlaywrightExclusions();
        await SetLocation();

        // get store from .fs-selected-store__name [data-store-id]
        var storeLocElement = await playwrightPage!.QuerySelectorAsync("span.fs-selected-store__name");
        var storeLocId = await storeLocElement!.GetAttributeAsync("data-store-id");

        Log(ConsoleColor.Yellow, $"Scraping {store.Name} ({storeLocId})");
        // verify store is correct
        if (storeLocId != store.Id) {
            Log(ConsoleColor.Red, $"Store ID mismatch: {storeLocId} != {store.Id}");
            throw new Exception("Store ID mismatch");
        }

        // Open up each URL and run the scraping function
        foreach (var category in categories) {
            await ScrapeCategory(category);
        }

        // Go to the paknsave website and select the matching store

        // For every category, scrape the products

        // For every product check if it exists in the database, if not, get the details and add it


        // Try clean up playwright browser and other resources, then end program
        try {
            Log(ConsoleColor.White, "Scraping Completed \n");
            await playwrightPage!.Context.CloseAsync();
            await playwrightPage.CloseAsync();
            await browser!.CloseAsync();
        }
        catch (System.Exception e) {
            LogError(e.ToString());
        }

        stopwatch.Stop();
        Log(ConsoleColor.Yellow, $"Scraped {store.Name} in {stopwatch.Elapsed}");
    }

    private async Task SetLocation() {
        // Set playwright geolocation using found latitude and longitude
        await playwrightPage!.Context.SetGeolocationAsync(
            new Geolocation() { Latitude = store.Latitude, Longitude = store.Longitude }
        );
        Log(ConsoleColor.Yellow,
            $"Selecting closest store using geo-location: ({store.Name}, {store.Latitude}, {store.Longitude})");
        await playwrightPage.Context.GrantPermissionsAsync(new string[] { "geolocation" });

        try {
            // Goto a page to trigger geolocation detection
            await playwrightPage.GotoAsync("https://www.paknsave.co.nz/shop/deals");

            // The server side code will detect the geolocation,
            //  and will automatically reload with the closet store set
            Thread.Sleep(5000);
            await playwrightPage.WaitForSelectorAsync("span.fs-price-lockup__cents");

        }
        catch (System.Exception e) {
            Log(ConsoleColor.Red, e.ToString());
            throw;
        }
    }

    public record ProductPrice {
        public string productId;
        public PriceHistoryStore.DatedPrice prices;
    }

    public async Task ScrapeCategory(Category category) {
        // paginate through the category pages and scrape each page with ScraperCategoryPage
        var page = 0;
        await playwrightPage!.GotoAsync(category.Url);
        Queue<string> pageUrls = new Queue<string>();
        pageUrls.Append(category.Url);
        while (pageUrls.Count > 0) {
            page++;
            Log(ConsoleColor.Yellow, $"Scraping page {page} of {category.Name} {category.Url}");
            var url = pageUrls.Dequeue();
            var scrapedProductPrices = await ScrapeCategoryPage(url);
            // persist these prices to the database
            // and if the product doesn't exist in the database, get the product details and create it
            foreach (var productPrice in scrapedProductPrices) {
                // TODO figure out how to persist
                // await PersistProductPrice(productPrice);
                var dbProduct = await ProductStore.GetProduct(productPrice.productId);
                if (dbProduct == null) {
                    // todo need full productid
                    var product = await GetProductDetails(productPrice.productId);
                    if (product != null) {
                        await ProductStore.UpsertProduct(product);
                    }
                }
            }

            // grab next page url
            var nextButton = await playwrightPage.QuerySelectorAsync(".fs-pagination__btn--next");
            if (nextButton != null) {
                var nextUrl = await nextButton.GetAttributeAsync("href");
                pageUrls.Append(nextUrl);
            }

            Thread.Sleep(secondsDelayBetweenPageScrapes * 1000);
        }
    }


    public async Task<List<ProductPrice>> ScrapeCategoryPage(string url) {
        var results = new List<ProductPrice>();
        // Try load page and wait for full content to dynamically load in
        try {
            await playwrightPage!.GotoAsync(url);
            await playwrightPage.WaitForSelectorAsync("span.fs-price-lockup__cents");


            // Query all product card entries, and log how many were found
            var productElements = await playwrightPage.QuerySelectorAllAsync("div.fs-product-card");
            Log(ConsoleColor.Yellow,
                $"{productElements.Count} Products Found \t" +
                $"Total Time Elapsed: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds.ToString().PadLeft(2, '0')})");

            // Loop through every found playwright element
            foreach (var productElement in productElements) {
                // Get dated price from element 
                var productPricing = await ScrapeProductElementToDatedPrice(productElement);
                if (productPricing != null) {
                    results.Add(productPricing);
                }
            }

            return results;
        }
        catch (System.TimeoutException) {
            Log(ConsoleColor.Red, "Unable to Load Web Page - timed out after 30 seconds");
        }
        catch (PlaywrightException e) {
            LogError("Unable to Load Web Page - " + e.Message);
        }
        catch (System.Exception e) {
            Console.Write(e.ToString());
            throw;
        }

        return results;
    }

    private async static Task<ProductPrice?> ScrapeProductElementToDatedPrice(IElementHandle productElement) {
        try {
            // Create a DateTime object for the current time, but set minutes and seconds to zero
            DateTime todaysDate = DateTime.UtcNow;
            todaysDate = new DateTime(
                todaysDate.Year,
                todaysDate.Month,
                todaysDate.Day,
                todaysDate.Hour,
                0,
                0
            );

            // Get product id from url element
            var aTag = await productElement.QuerySelectorAsync("a");
            string? productUrl = await aTag!.GetAttributeAsync("href");
            // e.g. 5260839 from https://www.paknsave.co.nz/shop/product/5260839_ea_000pns?name=salad-sprouts 
            string productId = productUrl!.Split("/").Last().Split("_").First();

            // Price
            var dollarSpan = await productElement.QuerySelectorAsync(".fs-price-lockup__dollars");
            string dollarString = await dollarSpan!.InnerHTMLAsync();

            var centSpan = await productElement.QuerySelectorAsync(".fs-price-lockup__cents");
            string centString = await centSpan!.InnerHTMLAsync();
            float currentPrice = float.Parse(dollarString + "." + centString);

            // TODO: get multibuy price and quantity

            // Create a DatedPrice for the current time and price
            var todaysDatedPrice = new PriceHistoryStore.DatedPrice(todaysDate, currentPrice);
            return new ProductPrice() {
                productId = productId,
                prices = todaysDatedPrice
            };
        }
        catch (Exception e) {
            Log(ConsoleColor.Red, $"Price scrape error: " + e.Message);
            // Return null if any exceptions occurred during scraping
            return null;
        }
    }

    public async Task<Product> GetProductDetails(string productId) {
        // The product details returned don't only depend on the URL but also the selected store location
        // If a product isn't stocked at a store the full info like SKU is not there

        // var aTag = await productElement.QuerySelectorAsync("a");
        // string? productUrl = await aTag!.GetAttributeAsync("href");
        // Console.WriteLine("Getting product details for https://www.paknsave.co.nz"+productUrl);

        // // 1. request a product page e.g. https://www.paknsave.co.nz/shop/product/5202207_kgm_000pns?name=fspns-honey-baked-ham#JTdCJTIyYWxnb2xpYUFuYWx5dGljcyUyMiUzQSU3QiUyMnNlYXJjaFF1ZXJ5SUQlMjIlM0ElMjJhYzc5Y2JiZGM4YjFhMWQ4MWI3MmE0MzA4YzU5YmJmNyUyMiUyQyUyMnNlYXJjaFBvc2l0aW9uJTIyJTNBMzklN0QlN0Q=

        // get product id from url e.g. 5202207_kgm_000pns
        // var productId = productUrl!.Split("/").Last().Split("?").First();
        // get name from query string
        var name = "food";
        var versionString = "YmW6_Ww8EVkmODFThIyLv";
        // 2. Grab the site version string from DOM:  grab what looks to be the version string (YmW6_Ww8EVkmODFThIyLv) from the DOM: <script src="/_next/static/YmW6_Ww8EVkmODFThIyLv/_buildManifest.js" defer=""></script>
        // 3. Request product details json
        var productDetailsUrl =
            $"https://www.paknsave.co.nz/_next/data/{versionString}/shop/product/{productId}.json?name={name}&productId={productId}";
        // Console.WriteLine("Getting product details from "+productDetailsUrl);
        // request 
        var httpclient = new HttpClient();
        httpclient.DefaultRequestHeaders.Add("Accept", "application/json");
        var requestProductDetails = await httpclient.GetAsync(productDetailsUrl);
        var productDetailsJson = await requestProductDetails.Content.ReadAsStringAsync();

        // 4. Parse json
        try {
            var product = JsonConvert.DeserializeObject<Product>(productDetailsJson);
            if (product != null) {
                return product;
            }
            else {
                throw new Exception("Error parsing product details json: " + productDetailsJson.Substring(0, 500));
            }
        }
        catch (Exception e) {
            Console.WriteLine("Error parsing product details json: " + e.Message);
            Console.Write(productDetailsJson.Substring(0, 500));
            throw;
        }
    }
    
    // Excludes playwright from downloading unwanted resources such as ads, trackers, images, etc.
    private async Task RoutePlaywrightExclusions(bool logToConsole = false) {
        // Define excluded types and urls to reject
        string[] typeExclusions = { "image", "stylesheet", "media", "font", "other" };
        string[] urlExclusions = {
            "googleoptimize.com", "gtm.js", "visitoridentification.js",
            "js-agent.newrelic.com", "challenge-platform"
        };
        List<string> exclusions = urlExclusions.ToList<string>();

        // Route with exclusions processed
        await playwrightPage!.RouteAsync("**/*", async route => {
            var req = route.Request;
            bool excludeThisRequest = false;
            string trimmedUrl = req.Url.Length > 120 ? req.Url.Substring(0, 120) + "..." : req.Url;

            foreach (string exclusion in exclusions) {
                if (req.Url.Contains(exclusion)) excludeThisRequest = true;
            }

            if (typeExclusions.Contains(req.ResourceType)) excludeThisRequest = true;

            if (excludeThisRequest) {
                if (logToConsole) Log(ConsoleColor.Red, $"{req.Method} {req.ResourceType} - {trimmedUrl}");
                await route.AbortAsync();
            }
            else {
                if (logToConsole) Log(ConsoleColor.White, $"{req.Method} {req.ResourceType} - {trimmedUrl}");
                await route.ContinueAsync();
            }
        });
    }
}
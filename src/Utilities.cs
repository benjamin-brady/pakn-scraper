using System.Text.RegularExpressions;
using static Scraper.Program;

namespace Scraper
{
    // Struct for manual overriding scraped product size and category found in 'ProductOverrides.txt'
    public struct SizeAndCategoryOverride
    {
        public string size;
        public string category;

        public SizeAndCategoryOverride(string size, string category)
        {
            this.size = size;
            this.category = category;
        }
    }

    // Struct for parsing URLs and additional data stored in 'Urls.txt'
    public struct CategorisedURL
    {
        public string url;
        public string[] categories;
        public int numPages;

        public CategorisedURL(string url, string[] categories, int numPages)
        {
            this.url = url;
            this.categories = categories;
            this.numPages = numPages;
        }
    }

    public partial class Utilities
    {
        // ParseLineToCategorisedURL()
        // ---------------------------
        // Parses a textLine containing a url and optional overridden category names.
        // Returns a CategorisedURL object or null if the line is invalid.


        // OptimiseURLQueryParameters
        // --------------------------
        // Parses urls and optimises query options for best results
        // Returns null if invalid

        public static string OptimiseURLQueryParameters(string url, string replaceQueryParamsWith)
        {
            string cleanURL = url;

            // If url contains 'search?', keep all query parameters
            if (url.ToLower().Contains("search?"))
            {
                return url;
            }

            // Else strip all query parameters
            else if (url.Contains('?'))
            {
                cleanURL = url.Substring(0, url.IndexOf('?')) + "?";
            }

            // If there were no existing query parameters, ensure a ? is added
            else cleanURL += "?";

            // Replace query parameters with optimised ones,
            //  such as limiting to certain sellers,
            //  or showing a higher number of products
            cleanURL += replaceQueryParamsWith;

            // Return cleaned url
            return cleanURL;
        }

        // DeriveUnitPriceString()
        // -----------------------
        // Derives unit quantity, unit name, and price per unit of a product,
        // Returns a string in format 450/ml

        public static string? DeriveUnitPriceString(string productSize, float productPrice)
        {
            // Return early if productSize is blank
            if (productSize == null || productSize.Length < 2) return null;

            string? matchedUnit = null;
            float? quantity = null;
            float? originalUnitQuantity = null;

            // If size is simply 'kg', process it as 1kg
            if (productSize == "kg" || productSize == "per kg")
            {
                quantity = 1;
                matchedUnit = "kg";
                originalUnitQuantity = 1;
            }
            else
            {
                // MatchedUnit is derived from product size, 450ml = ml
                matchedUnit = string.Join("", Regex.Matches(productSize.ToLower(), @"(g|kg|ml|l)\b"));

                // Quantity is derived from product size, 450ml = 450
                // Can include decimals, 1.5kg = 1.5
                try
                {
                    string quantityMatch = string.Join("", Regex.Matches(productSize, @"(\d|\.)"));
                    quantity = float.Parse(quantityMatch);
                    originalUnitQuantity = quantity;
                }
                catch (System.Exception)
                {
                    // If quantity cannot be parsed, the function will return null
                }
            }

            if (matchedUnit.Length > 0 && quantity > 0)
            {
                // Handle edge case where size contains a 'multiplier x sub-unit' - eg. 4 x 107mL
                string matchMultipliedSizeString = Regex.Match(productSize, @"\d+\s?x\s?\d+").ToString();
                if (matchMultipliedSizeString.Length > 2)
                {
                    int multiplier = int.Parse(matchMultipliedSizeString.Split("x")[0].Trim());
                    int subUnitSize = int.Parse(matchMultipliedSizeString.Split("x")[1].Trim());
                    quantity = multiplier * subUnitSize;
                    originalUnitQuantity = quantity;
                    matchedUnit = matchedUnit.ToLower().Replace("x", "");
                    //Log(ConsoleColor.DarkGreen, productSize + " = (" + quantity + ") (" + matchedUnit + ")");
                }

                // Handle edge case where size is in format '72g each 5pack'
                matchMultipliedSizeString = Regex.Match(productSize, @"\d+(g|ml)\seach\s\d+pack").ToString();
                if (matchMultipliedSizeString.Length > 2)
                {
                    int multiplier = int.Parse(matchMultipliedSizeString.Split("each")[1].Trim());
                    int subUnitSize = int.Parse(matchMultipliedSizeString.Split("each")[0].Trim());
                    quantity = multiplier * subUnitSize;
                    originalUnitQuantity = quantity;
                    matchedUnit = matchedUnit.ToLower().Replace("each", "");
                    //Log(ConsoleColor.DarkGreen, productSize + " = (" + quantity + ") (" + matchedUnit + ")");
                }

                // If units are in grams, normalize quantity and convert to /kg
                if (matchedUnit == "g")
                {
                    quantity = quantity / 1000;
                    matchedUnit = "kg";
                }

                // If units are in mL, normalize quantity and convert to /L
                if (matchedUnit == "ml")
                {
                    quantity = quantity / 1000;
                    matchedUnit = "L";
                }

                // Capitalize L for Litres
                if (matchedUnit == "l") matchedUnit = "L";

                // Set per unit price, rounded to 2 decimal points
                string roundedUnitPrice = Math.Round((decimal)(productPrice / quantity), 2).ToString();
                //Console.WriteLine(productPrice + " / " + quantity + " = " + roundedUnitPrice + "/" + matchedUnit);

                // Return in format '450g cheese' = '0.45/kg/450'
                return roundedUnitPrice + "/" + matchedUnit + "/" + originalUnitQuantity;
            }
            return null;
        }

        // Log()
        // -----
        // Shorthand function for logging with provided colour

        public static void Log(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        // LogError()
        // ----------
        // Shorthand function for logging with red colour
        public static void LogError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
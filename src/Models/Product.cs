using Newtonsoft.Json;

namespace Scraper.Models;

/**
 *   "name": "9 Grains Crispbread",
  "brand": "Arnott's Vita-Weat",
  "description": "Made with 100% Wholegrains. 100% Natural. 4 Crispbreads = 40% of Your Daily Wholegrains. Contains Soy, Gluten Containing Cereals. May contain traces of Egg, Milk, Peanut, Sesame, Tree Nut.",
  "unitOfMeasure": "EA",
  "price": 319,
  "nonLoyaltyCardPrice": 319,
  "productId": "5009687-EA-000",
  "weighable": {},
  "sku": "9310072014753",
  "comparativePricePerUnit": 128,
  "comparativeUnitQuantity": 100,
  "comparativeUnitQuantityUoM": "g",
  "saleType": "UNITS",
  "ingredientStatement": "Wholegrains (86%) (Wheat, Barley, Rye, Corn), Seeds (5%) (Canola, Linseed, Poppy, Sunflower Kernels), Vegetable Oil, Salt, Sugar, Soy.",
  "fsIngredientStatement": "Wholegrains (86%) (**Wheat**, **Barley**, **Rye**, Corn), Seeds (5%) (Canola, Linseed, Poppy, Sunflower Kernels), Vegetable Oil, Salt, Sugar, **Soy**.",
  "allergenStatement": "Contains Wheat, Gluten, Soy. Allergen May Be Present: Egg, Milk, Peanuts, Sesame, Tree Nuts",
    "inStoreMadeProduct": false,
  "fsValidation": {
    "allergens": {
      "status": "PASSED",
      "pealStatus": "PASSED"
    }
  },
  "restrictedFlag": false,
  "netContentUOM": "250g",
  "displayName": "250g",
  "height": 94,
  "width": 227,
  "categories": [
    "Biscuits & Crackers",
    "Crackers"
  ],
  "availability": [
    "IN_STORE",
    "ONLINE"
  ],
  "originRegulated": false,
  "categoryTrees": [
    {
      "level0": "Pantry",
      "level1": "Biscuits & Crackers",
      "level2": "Crackers"
    }
  ],
    "images": {
    "primaryImages": {
      "100px": "https://a.fsimg.co.nz/product/retail/fan/image/100x100/5009687.png",
      "200px": "https://a.fsimg.co.nz/product/retail/fan/image/200x200/5009687.png",
      "300px": "https://a.fsimg.co.nz/product/retail/fan/image/300x300/5009687.png",
      "400px": "https://a.fsimg.co.nz/product/retail/fan/image/400x400/5009687.png",
      "500px": "https://a.fsimg.co.nz/product/retail/fan/image/500x500/5009687.png"
    },
 */
public class Product {
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("brand")]
    public string Brand;
    
    [JsonProperty("description")]
    public string Description;
    
    [JsonProperty("unitOfMeasure")]
    public string UnitOfMeasure;
    
    [JsonProperty("price")]
    public float Price;
    
    [JsonProperty("nonLoyaltyCardPrice")]
    public float NonLoyaltyCardPrice;
    
    [JsonProperty("productId")]
    public string ProductId;
    
    [JsonProperty("weighable")]
    public object? Weighable;
    
    [JsonProperty("barcode")]
    public long? Barcode {
      get => string.IsNullOrWhiteSpace(Sku) ? null : long.Parse(Sku);
      set => Sku = (value ?? 0).ToString(); 
    }
    
    [JsonProperty("sku")]
    public string Sku;
    
    [JsonProperty("comparativePricePerUnit")]
    public float ComparativePricePerUnit;
    
    [JsonProperty("comparativeUnitQuantity")]
    public float ComparativeUnitQuantity;
    
    [JsonProperty("comparativeUnitQuantityUoM")]
    public string ComparativeUnitQuantityUoM;
    
    [JsonProperty("saleType")]
    public string SaleType;
    
    [JsonProperty("ingredientStatement")]
    public string IngredientStatement;
    
    [JsonProperty("fsIngredientStatement")]
    public string FsIngredientStatement;
    
    [JsonProperty("allergenStatement")]
    public string AllergenStatement;
    
    [JsonProperty("inStoreMadeProduct")]
    public bool InStoreMadeProduct;
    
    [JsonProperty("fsValidation")]
    public object? FsValidation;
    
    [JsonProperty("restrictedFlag")]
    public bool RestrictedFlag;
    
    [JsonProperty("netContentUOM")]
    public string NetContentUOM;
    
    [JsonProperty("displayName")]
    public string DisplayName;
    
    [JsonProperty("height")]
    public int Height;
    
    [JsonProperty("width")] 
    public int Width;
    
    [JsonProperty("categories")]
    public string[] Categories;
    
    [JsonProperty("availability")]
    public string[] Availability;
    
    [JsonProperty("originRegulated")]
    public bool OriginRegulated;
    
    [JsonProperty("categoryTrees")]
    public object[]? CategoryTrees;
    
    [JsonProperty("images")]
    public object[]? Images;
    
    public Product() {
        this.Name = "";
        this.Brand = "";
        this.Description = "";
        this.UnitOfMeasure = "";
        this.Price = 0;
        this.NonLoyaltyCardPrice = 0;
        this.ProductId = "";
        this.Sku = "";
        this.ComparativePricePerUnit = 0;
        this.ComparativeUnitQuantity = 0;
        this.ComparativeUnitQuantityUoM = "";
        this.SaleType = "";
        this.IngredientStatement = "";
        this.FsIngredientStatement = "";
        this.AllergenStatement = "";
        this.InStoreMadeProduct = false;
        this.FsValidation = null;
        this.RestrictedFlag = false;
        this.NetContentUOM = "";
        this.DisplayName = "";
        this.Height = 0;
        this.Width = 0;
        this.Categories = new string[0];
        this.Availability = new string[0];
        this.OriginRegulated = false;
        this.CategoryTrees = null;
        this.Images = null;
    }
}
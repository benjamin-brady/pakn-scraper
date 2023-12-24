using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Scraper.Models;
using static Scraper.Program;
using static Scraper.Utilities;

namespace Scraper
{
    public partial class ProductStore
    {
        // CosmosDB singletons
        public static CosmosClient? cosmosClient;
        public static Database? database;
        public static Container? cosmosContainer;

        public static async Task<bool> EstablishConnection(string db, string partitionKey, string container)
        {
            try
            {
                // Read from appsettings.json or appsettings.local.json
                cosmosClient = new CosmosClient(
                    accountEndpoint: config!.GetRequiredSection("COSMOS_ENDPOINT").Get<string>(),
                    authKeyOrResourceToken: config!.GetRequiredSection("COSMOS_KEY").Get<string>()!
                );

                database = cosmosClient.GetDatabase(id: db);

                cosmosContainer = await database.CreateContainerIfNotExistsAsync(
                    id: container,
                    partitionKeyPath: partitionKey,
                    throughput: 400
                );

                Log(ConsoleColor.Yellow, $"\n(Connected to CosmosDB) {cosmosClient.Endpoint}");
                return true;
            }
            catch (CosmosException e)
            {
                LogError(e.GetType().ToString());
                Log(ConsoleColor.Red,
                "Error Connecting to CosmosDB - check appsettings.json, endpoint or key may be expired");
                return false;
            }
            catch (HttpRequestException e)
            {
                LogError(e.GetType().ToString());
                Log(ConsoleColor.Red,
                "Error Connecting to CosmosDB - check firewall and internet status");
                return false;
            }
            catch (Exception e)
            {
                LogError(e.GetType().ToString());
                Log(ConsoleColor.Red,
                "Error Connecting to CosmosDB - make sure appsettings.json is created and contains:");
                Log(ConsoleColor.White,
                    "{\n" +
                    "\t\"COSMOS_ENDPOINT\": \"<your cosmosdb endpoint uri>\",\n" +
                    "\t\"COSMOS_KEY\": \"<your cosmosdb primary key>\"\n" +
                    "}\n"
                );
                return false;
            }
        }

        public static async Task UpsertProduct(Product product) {
            var response = await cosmosContainer!.UpsertItemAsync<Product>(
                product,
                new PartitionKey(product.ProductId)
            );
            
            if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                Log(ConsoleColor.Green, $"Upserted {product.ProductId} - {product.Name}");
            } else {
                Log(ConsoleColor.Red, $"Failed to upsert {product.ProductId} - {product.Name}");
            }
        }

        
        public static async Task<Product?> GetProduct(string id)
        {
            var response = await cosmosContainer!.ReadItemAsync<Product>(
                id,
                new PartitionKey(id)
            );

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Get product from CosmosDB resource
                Product dbProduct = response.Resource;
                Console.WriteLine($"  Found {dbProduct.ProductId} - {dbProduct.Name}");
                return dbProduct;
            }

            Console.WriteLine($"  Product {id} not found");
            return null;
        }
    }
}
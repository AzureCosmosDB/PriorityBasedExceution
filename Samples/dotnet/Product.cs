using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Bogus;

public class Product
{
    public Product()
    {
    }

    public Product(string id, string category, string name, int quantity, decimal price, bool clearance)
    {
        this.id = id;
        this.Category = category;
        this.Name = name;
        this.Quantity = quantity;
        this.Price = price;
        this.Clearance = clearance;
    }

    
    [JsonProperty(PropertyName = "id")]
    public string id { get; set; }
    [JsonProperty(PropertyName = "category")]
    public string Category { get; set; }
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }
    [JsonProperty(PropertyName = "quantity")]
    public int Quantity { get; set; }
    [JsonProperty(PropertyName = "price")]
    public decimal Price {get; set;}
    [JsonProperty(PropertyName = "clearance")]
    public bool Clearance {get; set;}

    public static List<Product> GenerateProducts(int numDocs, int idCounter)
    {
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.id, f => "id_" + idCounter++)
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Quantity, f => f.Random.Number(1, 100))
            .RuleFor(p => p.Price, f => f.Random.Decimal(1, 50000))
            .RuleFor(p => p.Clearance, f => f.Random.Bool());

        List<Product> products = new List<Product>();
        for (int i = 0; i < numDocs; i++)
        {
            Product fakeProduct = productFaker.Generate();
            products.Add(fakeProduct);
        }
        return products;
    }
    
}
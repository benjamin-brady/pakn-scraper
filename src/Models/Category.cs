namespace Scraper.Models;

public class Category
{
    public string Name;
    public string Url;
    
    public Category(string name, string url)
    {
        Name = name;
        Url = url;
    }
    
    public Category()
    {
        Name = "";
        Url = "";
    }
}
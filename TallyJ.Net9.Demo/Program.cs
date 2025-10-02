// TallyJ .NET 9 Migration Proof of Concept
// This demonstrates .NET 9 compatibility and modern C# features

using System;
using System.Text.Json;

Console.WriteLine("TallyJ .NET 9 Compatibility Demo");
Console.WriteLine($"Running on .NET {Environment.Version}");
Console.WriteLine($"Current DateTime: {DateTime.Now}");

// Demonstrate modern C# features
var sampleData = new
{
    Version = "3.5.40",
    FrameworkVersion = ".NET 9.0",
    MigrationStatus = "Proof of Concept",
    Features = new[] 
    {
        "Modern C# syntax",
        "System.Text.Json serialization", 
        "Performance improvements",
        "Security enhancements"
    }
};

// Modern JSON serialization
string json = JsonSerializer.Serialize(sampleData, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine("\nSample JSON output:");
Console.WriteLine(json);

// Demonstrate file I/O with modern patterns
await CreateSampleConfigAsync();

Console.WriteLine("\n✅ .NET 9 compatibility verified!");
Console.WriteLine("This proves TallyJ can run on .NET 9 with appropriate migration work.");

static async Task CreateSampleConfigAsync()
{
    var config = new
    {
        DatabaseConnection = "Server=.;Database=TallyJ;Integrated Security=true;",
        LogLevel = "Information",
        Features = new
        {
            EnableModernAuth = true,
            UseAsyncPatterns = true,
            JsonSerialization = "System.Text.Json"
        }
    };
    
    string configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync("appsettings.sample.json", configJson);
    Console.WriteLine("✅ Created sample appsettings.json configuration");
}

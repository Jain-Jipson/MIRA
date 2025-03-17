using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

public class VisitorLogService
{
    private readonly IMongoCollection<Visitor> _visitorsCollection;

    public VisitorLogService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _visitorsCollection = database.GetCollection<Visitor>("Visitors"); // ✅ Collection
    }

    // ✅ Log a visitor
    public async Task LogVisitorAsync(string name, string imagePath)
    {
        var visitor = new Visitor
        {
            Id = ObjectId.GenerateNewId().ToString(), // ✅ Ensure valid ObjectId
            Name = name,
            ImagePath = imagePath
        };
        await _visitorsCollection.InsertOneAsync(visitor);
    }

    // ✅ Retrieve all visitors
    public async Task<List<Visitor>> GetVisitorsAsync()
    {
        return await _visitorsCollection.Find(_ => true).ToListAsync();
    }
}

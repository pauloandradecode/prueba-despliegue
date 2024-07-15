using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
using System.Linq;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public Mock<IMongoCollection<BsonDocument>> MockCollection { get; }
    public Mock<IMongoDatabase> MockDatabase { get; }
    public Mock<IMongoClient> MockClient { get; }

    public CustomWebApplicationFactory()
    {
        MockCollection = new Mock<IMongoCollection<BsonDocument>>();
        MockDatabase = new Mock<IMongoDatabase>();
        MockClient = new Mock<IMongoClient>();

        MockDatabase.Setup(db => db.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                    .Returns(MockCollection.Object);
        MockClient.Setup(client => client.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                  .Returns(MockDatabase.Object);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IMongoClient));
            services.AddSingleton(MockClient.Object);

            services.RemoveAll(typeof(IMongoDatabase));
            services.AddSingleton(MockDatabase.Object);

            services.RemoveAll(typeof(IMongoCollection<BsonDocument>));
            services.AddSingleton(MockCollection.Object);
        });
    }
}

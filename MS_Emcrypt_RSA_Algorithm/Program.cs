using Encriptacion_RSA.Middleware;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThirdParty.Json.LitJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Leer configuracion de MongoDB desde appsettings.json
var mongoSettings = builder.Configuration.GetSection("MongoDB");

string connectionString = mongoSettings["ConnectionString"];
string databaseName = mongoSettings["DatabaseName"];
string keyCollectionName = mongoSettings["keys_collection"];

// Configura el cliente de MongoDB para microservicio
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
    var settings = MongoClientSettings.FromConnectionString(connectionString);
    return new MongoClient(settings);
});
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<BsonDocument>(keyCollectionName);
});

var app = builder.Build();

//app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.MapPost("/keys", async (HttpContext context, IMongoCollection<BsonDocument> keysCollection) =>
{
    try
    {
        var keyRequest = await JsonSerializer.DeserializeAsync<KeyRequestModel>(context.Request.Body);
        if (keyRequest == null || keyRequest.ClientId == 0)
        {
            return Results.Json(new ResponseModel { Code = 400, Menssage = "Invalid ClientId" });
        }

        var filter = Builders<BsonDocument>.Filter.Eq("IdCliente", keyRequest.ClientId);
        var publicKeyDocument = (await keysCollection.FindAsync(filter)).FirstOrDefault();

        if (publicKeyDocument != null)
        {
            return Results.Json(new ResponseModel { Code = 400, Menssage = "ClientId found" });
        }

        // Generar las claves RSA
        RSA rsa = RSA.Create();
        var publicKeyPEM = ExportPublicKeyToPEM(rsa);
        var privateKeyPEM = ExportPrivateKeyToPEM(rsa);

        // Guardar en MongoDB
        var document = new BsonDocument
        {
            { "FechaRegistro", DateTime.UtcNow },
            { "IdCliente", keyRequest.ClientId },
            { "PublicKey", publicKeyPEM },
            { "PrivateKey", privateKeyPEM },
            { "Activo", true }
        };

        await keysCollection.InsertOneAsync(document);

        return Results.Json(new ResponseModel { Code = 200, Menssage = "Success", Object = new { PublicKey = publicKeyPEM } });
    }
    catch (Exception ex)
    {
        return Results.Json(new ResponseModel { Code = 500, Menssage = "Server Internal Error" });
    }
})
.Accepts<KeyRequestModel>("application/json")
.Produces<ResponseModel>(200)
.Produces<ResponseModel>(400)
.Produces<ResponseModel>(500)
.WithOpenApi();

app.Run();

// Helper methods to convert RSA keys to PEM format
string ExportPublicKeyToPEM(RSA rsa)
{
    var publicKey = rsa.ExportSubjectPublicKeyInfo();
    return Convert.ToBase64String(publicKey);
}

string ExportPrivateKeyToPEM(RSA rsa)
{
    var privateKey = rsa.ExportPkcs8PrivateKey();
    return Convert.ToBase64String(privateKey);
}

RSA ImportPublicKeyFromPEM(string pem)
{
    var publicKey = Convert.FromBase64String(pem);
    var rsa = RSA.Create();
    rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
    return rsa;
}

RSA ImportPrivateKeyFromPEM(string pem)
{
    var privateKey = Convert.FromBase64String(pem);
    var rsa = RSA.Create();
    rsa.ImportPkcs8PrivateKey(privateKey, out _);
    return rsa;
}

// Modelo de datos
public class KeyRequestModel
{
    [JsonPropertyName("clientId")]
    public int ClientId { get; set; }
}
public class ResponseModel
{
    public int Code { get; set; }
    public string Menssage { get; set; }
    public object Object { get; set; }
}

public partial class Program { } // Necesario para WebApplicationFactory
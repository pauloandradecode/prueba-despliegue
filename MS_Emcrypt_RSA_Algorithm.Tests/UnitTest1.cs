using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

public class ApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostKeys_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var keyRequest = new KeyRequestModel { ClientId = 1 };

        // Mock MongoDB Collection Find response
        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<System.Threading.CancellationToken>())).Returns(false);
        _factory.MockCollection.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<FindOptions<BsonDocument, BsonDocument>>(), default))
                               .ReturnsAsync(mockCursor.Object);

        // Act
        var response = await client.PostAsJsonAsync("/keys", keyRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
        Assert.Equal(200, responseModel.Code);
        Assert.Equal("Success", responseModel.Menssage);
    }

    [Fact]
    public async Task PostKeys_ShouldReturnBadRequest_WhenClientIdIsZero()
    {
        // Arrange
        var client = _factory.CreateClient();
        var keyRequest = new KeyRequestModel { ClientId = 0 };

        // Act
        var response = await client.PostAsJsonAsync("/keys", keyRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
        Assert.Equal(400, responseModel.Code);
        Assert.Equal("Invalid ClientId", responseModel.Menssage);
    }

    [Fact]
    public async Task PostKeys_ShouldReturnBadRequest_WhenClientIdExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var keyRequest = new KeyRequestModel { ClientId = 1 };

        // Crear un documento simulado que represente un cliente existente
        var existingDocument = new BsonDocument { { "IdCliente", 1 } };

        // Mock MongoDB Collection Find response
        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<System.Threading.CancellationToken>()))
                  .Returns(true) // Indica que hay más elementos
                  .Returns(false); // Indica que no hay más elementos
        mockCursor.Setup(x => x.Current)
                  .Returns(new List<BsonDocument> { existingDocument });

        _factory.MockCollection.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<BsonDocument>>(),
                                                       It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
                                                       default))
                               .ReturnsAsync(mockCursor.Object);

        // Act
        var response = await client.PostAsJsonAsync("/keys", keyRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel>();
        Assert.Equal(400, responseModel.Code);
        Assert.Equal("ClientId found", responseModel.Menssage);
    }
}

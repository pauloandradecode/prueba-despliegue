using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

namespace Encriptacion_RSA.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMongoCollection<BsonDocument> _collection;

        public RequestResponseLoggingMiddleware(RequestDelegate next, IMongoDatabase database)
        {
            _next = next;
            _collection = database.GetCollection<BsonDocument>("RequestResponseLogs");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            HttpRequest request = context.Request;

            if (!context.Request.Path.ToString().ToLower().Contains("swagger") && !context.Request.Path.ToString().ToLower().Contains("favicon"))
            {
                // Guardar el request en MongoDB
                var requestDocument = await FormatRequest(context.Request);

                await _collection.InsertOneAsync(requestDocument);

                // Interceptar y leer el response
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    try
                    {
                        await _next(context);

                        var response = await FormatResponse(context.Response);
                        await LogResponse(requestDocument["_id"], response);
                    }
                    catch (Exception ex)
                    {
                        await LogException(requestDocument["_id"], ex);
                        throw; // Re-throw the exception after logging it
                    }

                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task<BsonDocument> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            return new BsonDocument
            {
                { "Status", "200" },
                { "Service", request.Host.ToString() },
                { "Method", request.Method },
                { "Path", request.Path.ToString() },
                { "Timestamp", BsonDateTime.Create(DateTime.UtcNow) },
                { "QueryString", request.QueryString.ToString() },
                { "Body", bodyAsText }
            };
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"StatusCode: {response.StatusCode}, Body: {text}";
        }

        private async Task LogResponse(BsonValue requestId, string response)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", requestId);
            var update = Builders<BsonDocument>.Update.Set("Response", response);
            await _collection.UpdateOneAsync(filter, update);
        }

        private async Task LogException(BsonValue requestId, Exception exception)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", requestId);
            var update = Builders<BsonDocument>.Update.Set("Exception", new BsonDocument
            {
                { "Message", exception.Message },
                { "StackTrace", exception.StackTrace },
                { "InnerException", exception.InnerException == null ? string.Empty : exception.InnerException?.ToString() }
            });
            await _collection.UpdateOneAsync(filter, update);

            update = Builders<BsonDocument>.Update.Set("Status", "500");
            await _collection.UpdateOneAsync(filter, update);
        }
    }
}

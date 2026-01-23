using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

namespace InventoryService.Embedding.Extensions
{
    public static class EmbeddingExtensions
    {
        public static IServiceCollection AddEmbeddingServices(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"];
            if (string.IsNullOrWhiteSpace(ollamaBaseUrl) || !Uri.TryCreate(ollamaBaseUrl, UriKind.Absolute, out var ollamaUri))
            {
                throw new InvalidOperationException("Ollama:BaseUrl must be configured as an absolute URI.");
            }

            builder.Services.AddOllamaChatCompletion("gpt-oss:20b", ollamaUri);
            builder.Services.AddOllamaEmbeddingGenerator("nomic-embed-text", ollamaUri);

            var qdrantHost = builder.Configuration["Qdrant:Host"];
            var qdrantPortValue = builder.Configuration["Qdrant:Port"];
            var qdrantUseHttpsValue = builder.Configuration["Qdrant:UseHttps"];
            var qdrantApiKey = builder.Configuration["Qdrant:ApiKey"];

            if (string.IsNullOrWhiteSpace(qdrantHost))
            {
                throw new InvalidOperationException("Qdrant:Host must be configured.");
            }

            if (!int.TryParse(qdrantPortValue, out var qdrantPort))
            {
                throw new InvalidOperationException("Qdrant:Port must be configured as an integer.");
            }

            var qdrantUseHttps = bool.TryParse(qdrantUseHttpsValue, out var useHttps) && useHttps;

            builder.Services.AddQdrantVectorStore(qdrantHost, qdrantPort, https: qdrantUseHttps, apiKey: string.IsNullOrWhiteSpace(qdrantApiKey) ? null : qdrantApiKey);

            return services;
        }
    }
}

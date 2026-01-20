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
            builder.Services.AddOllamaChatCompletion("gpt-oss:20b", new Uri("http://localhost:11434"));
            builder.Services.AddOllamaEmbeddingGenerator("nomic-embed-text", new Uri("http://localhost:11434"));
            builder.Services.AddQdrantVectorStore("localhost");

            return services;
        }
    }
}

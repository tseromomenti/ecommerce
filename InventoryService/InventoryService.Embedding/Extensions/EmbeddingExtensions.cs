using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryService.Embedding.Extensions
{
    public static class EmbeddingExtensions
    {
        public static IServiceCollection AddEmbeddingServices(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            builder.Services.AddOllamaEmbeddingGenerator("gpt-oss:20b", new Uri("http://localhost:11434"));
            // Implementation for adding embedding services


            return null;
        }
    }
}

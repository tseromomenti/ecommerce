using Microsoft.SemanticKernel.ChatCompletion;

namespace InventoryService.Api.Endpoints;

internal static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("ChatAi", async (IChatCompletionService chatCompletion) =>
        {
            var result = await chatCompletion.GetChatMessageContentsAsync("Why is sky blue");
            return Results.Ok(result);
        });

        return endpoints;
    }
}

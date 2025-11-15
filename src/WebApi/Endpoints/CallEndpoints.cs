using MediatR;
using Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.InitiateCall;
using Vonage_SSW_Workshop.WebApi.Extensions;

namespace Vonage_SSW_Workshop.WebApi.Endpoints;

public static class CallEndpoints
{
    public static void MapCallEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup("calls");

        group
            .MapPost("/initiate", async (
                ISender sender,
                InitiateCallCommand command,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(
                    callId => TypedResults.Ok(new { CallId = callId }),
                    CustomResult.Problem);
            })
            .WithName("InitiateCall")
            .ProducesPost();
    }
}
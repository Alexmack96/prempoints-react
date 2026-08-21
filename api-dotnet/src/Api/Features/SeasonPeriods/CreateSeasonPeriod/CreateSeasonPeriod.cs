using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.SeasonPeriods.CreateSeasonPeriod;

public static class CreateSeasonPeriod
{
    public record Command(int GameweekNumber, DateOnly PeriodStartDate, DateOnly PeriodEndDate, int SeasonStartYear) : IRequest<Result<SeasonPeriodDto>>;
    public record Request(int GameweekNumber, DateOnly PeriodStartDate, DateOnly PeriodEndDate, int SeasonStartYear);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("seasonPeriods", Handle)
               .WithName("CreateSeasonPeriod")
               .Produces<SeasonPeriodDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .WithTags("SeasonPeriods");
        }

        public static async Task<IResult> Handle([FromBody] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.GameweekNumber, request.PeriodStartDate, request.PeriodEndDate, request.SeasonStartYear);

            var result = await sender.Send(command, ct);

            return result.ToMinimalApiResult();
        }
    }
}

using MediatR;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Features.Flags.CreateFlag;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Flags.EvaluateFlag;

public record EvaluateFlagRequest(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey,
    string FlagKey,
    Dictionary<string, string>? Traits // v1'de kullanmayacağız ama interface'e koyalım
): IRequest<Response<EvaluateFlagResponseDto>>;

public record EvaluateFlagResponseDto(
    string FlagKey,
    bool IsEnabled,
    string? VariantKey,
    string? PayloadJson   // Config için ileride
);

public class EvaluateFlagHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<EvaluateFlagRequest, Response<EvaluateFlagResponseDto>>
{
    public Task<Response<EvaluateFlagResponseDto>> Handle(EvaluateFlagRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
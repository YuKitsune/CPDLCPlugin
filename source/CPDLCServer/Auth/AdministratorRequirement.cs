using CPDLCServer.Auth.Administrators;
using Microsoft.AspNetCore.Authorization;

namespace CPDLCServer.Auth;

// Only an administrator can open the Configuration area.
// The handler reads the database for each request, so a removed administrator loses access immediately.
public class AdministratorRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Administrator";
}

public class AdministratorRequirementHandler(IAdministratorRepository administratorRepository)
    : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        var cid = context.User.GetCid();
        if (cid is null)
            return;

        if (await administratorRepository.FindByCid(cid, CancellationToken.None) is not null)
            context.Succeed(requirement);
    }
}

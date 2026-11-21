using MediatR;

namespace CPDLCServer.Auth.Administrators;

public class UpdateAdministratorNameCommandHandler(IAdministratorRepository administratorRepository)
    : IRequestHandler<UpdateAdministratorNameCommand>
{
    public async Task Handle(UpdateAdministratorNameCommand request, CancellationToken cancellationToken)
    {
        var administrator = await administratorRepository.FindByCid(request.Cid, cancellationToken);
        if (administrator is null || administrator.Name == request.Name)
            return;

        administrator.Name = request.Name;
    }
}

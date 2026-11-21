using MediatR;

namespace CPDLCServer.Auth.Administrators;

// The setup adds the first administrator
public record CreateFirstAdministratorCommand(int Cid) : IRequest;

using MediatR;

namespace CPDLCServer.Auth.Administrators;

// The current user cannot remove their own access, so that the server always has an administrator
public record RemoveAdministratorCommand(int Cid, int CurrentUserCid) : IRequest;

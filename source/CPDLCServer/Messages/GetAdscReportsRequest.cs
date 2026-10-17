using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetAdscReportsRequest : IRequest<GetAdscReportsResult>;

public record GetAdscReportsResult(AdscReportDto[] Reports);

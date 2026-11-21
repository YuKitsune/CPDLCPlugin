using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.AdscContracts.Reports;

public record GetAdscReportsRequest : IRequest<GetAdscReportsResult>;

public record GetAdscReportsResult(AdscReportDto[] Reports);

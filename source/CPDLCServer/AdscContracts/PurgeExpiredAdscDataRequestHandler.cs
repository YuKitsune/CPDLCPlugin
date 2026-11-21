using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class PurgeExpiredAdscDataRequestHandler(
    IAdscMessageLogRepository adscMessageLogRepository,
    IClock clock)
    : IRequestHandler<PurgeExpiredAdscDataRequest>
{
    readonly TimeSpan _messageLogRetention = TimeSpan.FromHours(24);

    // The ADS-C reports are not purged here. Each contract keeps only its latest report, and the report expires
    // soon after the next periodic reports do not arrive (ExpireAdscReportsRequestHandler).
    public async Task Handle(PurgeExpiredAdscDataRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();

        await adscMessageLogRepository.RemoveOlderThan(now - _messageLogRetention, cancellationToken);
    }
}

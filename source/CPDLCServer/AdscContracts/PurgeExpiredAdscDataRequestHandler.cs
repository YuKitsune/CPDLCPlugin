using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.Infrastructure;
using MediatR;

namespace CPDLCServer.AdscContracts;

public class PurgeExpiredAdscDataRequestHandler(
    IAdscMessageLogRepository adscMessageLogRepository,
    IClock clock)
    : IRequestHandler<PurgeExpiredAdscDataRequest>
{
    // The privacy policy shows this value
    public static readonly TimeSpan MessageLogRetention = TimeSpan.FromHours(24);

    // The ADS-C reports are not purged here. Each contract keeps only its latest report, and the report expires
    // soon after the next periodic reports do not arrive (ExpireAdscReportsRequestHandler).
    public async Task Handle(PurgeExpiredAdscDataRequest request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();

        await adscMessageLogRepository.RemoveOlderThan(now - MessageLogRetention, cancellationToken);
    }
}

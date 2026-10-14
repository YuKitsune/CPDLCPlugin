namespace CPDLCServer.Model;

// The aircraft does not acknowledge a request. The first report after the request accepts it.
public enum AdscContractState
{
    Sent,
    Accepted,
    Rejected
}

public sealed record TransferRequest(
    string FromAccount,
    string ToAccount,
    decimal Amount,
    string Currency);
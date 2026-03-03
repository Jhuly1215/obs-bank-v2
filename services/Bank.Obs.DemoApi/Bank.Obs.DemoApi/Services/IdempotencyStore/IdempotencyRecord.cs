namespace Bank.Obs.DemoApi.Services;

public sealed record IdempotencyRecord(
	string Key,
	string TransactionId,
	string Status,
	string ResponsePayload,
	DateTime CreatedAt);
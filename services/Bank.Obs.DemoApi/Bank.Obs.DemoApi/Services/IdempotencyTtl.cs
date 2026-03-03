using System;

namespace Bank.Obs.DemoApi.Services;

public sealed record IdempotencyTtl(TimeSpan Value);
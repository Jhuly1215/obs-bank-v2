using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Auth;

public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // En dev NO queremos “todo abierto”: si no mandas Authorization -> 401.
        if (!Request.Headers.TryGetValue("Authorization", out var auth) || auth.Count == 0)
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));

        var claims = new List<Claim>
        {
            new("sub", "dev-user"),
            new("scope", "transactions.transfer") // satisface tu policy CanTransfer
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
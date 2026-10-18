using System.Security.Cryptography;
using System.Text;

namespace WebUI.Services;

/// <summary>
/// The dashboard password, read once from the host configuration (<c>Dashboard:Password</c>, or the
/// <c>Dashboard__Password</c> environment variable). Only a hash of it is kept in memory.
/// </summary>
public sealed class DashboardAccess
{
    public const string PasswordConfigurationKey = "Dashboard:Password";
    public const string AllowRemoteWithoutPasswordConfigurationKey = "Dashboard:AllowRemoteWithoutPassword";
    public const string PasswordStampClaimType = "seth:password-stamp";

    private readonly byte[]? _PasswordHash;

    public DashboardAccess(IConfiguration configuration)
    {
        string? password = configuration[PasswordConfigurationKey];
        if (!string.IsNullOrEmpty(password))
        {
            _PasswordHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            PasswordStamp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("seth-dashboard-session:" + password)))[..16];
        }

        AllowRemoteWithoutPassword = configuration.GetValue<bool>(AllowRemoteWithoutPasswordConfigurationKey);
    }

    public bool IsPasswordRequired => _PasswordHash is not null;

    /// <summary>Without a password the dashboard only answers this machine, unless this is set.</summary>
    public bool AllowRemoteWithoutPassword { get; }

    /// <summary>
    /// Stored in the sign-in cookie. It changes with the password, so changing the password signs everyone out.
    /// </summary>
    public string? PasswordStamp { get; }

    public bool Verify(string? candidate)
    {
        if (_PasswordHash is null || candidate is null)
        {
            return false;
        }

        byte[] candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
        return CryptographicOperations.FixedTimeEquals(_PasswordHash, candidateHash);
    }
}

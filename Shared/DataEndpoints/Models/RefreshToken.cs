using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Models;
public class RefreshSession
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string RefreshTokenHash { get; set; } = default!;
    public string JwtId { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string DeviceInfo { get; set; } = "unknown";
}

public class RefreshToken
{
    public Guid Id { get; set; } // unikalny identyfikator refresh tokena
    public string Token { get; set; } = string.Empty;
    public string? AccessTokenJti { get; set; } // ID przypisanego access tokena (opcjonalnie)
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? DeviceInfo { get; set; } // np. "iPhone 15", fingerprint, IP hash, user-agent
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; } // np. jeśli został odświeżony
    public bool IsActive => !IsRevoked && DateTime.UtcNow <= ExpiresAt;
}
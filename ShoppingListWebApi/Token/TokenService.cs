using EFDataBase;
using FirebaseDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using Shared.DataEndpoints.Models.Requests;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITokenEndpoint _tokenEndpoint;

    public TokenService(IConfiguration configuration, IServiceProvider serviceProvider, ITokenEndpoint tokenEndpoint)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _tokenEndpoint = tokenEndpoint;
    }


    public async Task<(string accessToken, string refreshToken)> GenerateTokens(int userId, string derviceId, int? tokenVersion = null)
    {
        var (accessToken, jti) = await GenerateAccessToken(userId);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenSession = new RefreshTokenSession
        {
            RefreshToken = refreshToken,
            AccessTokenJti = jti,
            UserId = userId.ToString(),
            Version = tokenVersion ?? 1,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            DeviceInfo = derviceId

        };

        await _tokenEndpoint.AddRefreshToken(userId, refreshTokenSession);

        return (accessToken, refreshToken);
    }

    public async Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync(int userId, string refreshToken)
    {

        var refreshTokenSessions = await _tokenEndpoint.GetRefreshTokens(userId);

        var refreshTokenSession = refreshTokenSessions.Where(a => a.RefreshToken == refreshToken).FirstOrDefault();
        
        if (refreshTokenSession is null || refreshTokenSession.IsRefreshTokenRevoked || refreshTokenSession.ExpiresAt < DateTime.UtcNow)
        {
            return (string.Empty, string.Empty);
        }


        var (accessToken, jti) = await GenerateAccessToken(userId);
        var refreshTokenNew = GenerateRefreshToken();

        var refreshTokenSessionNew = new RefreshTokenSession
        {
            RefreshToken = refreshTokenNew, 
            AccessTokenJti = jti,
            UserId = userId.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),

        };

        await _tokenEndpoint.ReplaceRefreshToken(userId, refreshTokenSession, refreshTokenSessionNew);

        return (accessToken, refreshTokenNew);
    }

    public async Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync2(int userId, string deviceId, string refreshToken, int version, CancellationToken cancellationToken)
    {

        version += 1;
        var (accessToken, jti) = await GenerateAccessToken(userId, version);
        var refreshTokenNew = GenerateRefreshToken();
        try
        {
            return await _tokenEndpoint.ReplaceRefreshToken2(userId, deviceId, refreshToken, accessToken, jti, version, refreshTokenNew, cancellationToken);
        }
        catch(Exception ex)
        {
            return ("", "");
        }
    }


    public async Task<bool> VerifyAcceessRefreshTokens(int userId, string jti, VerifyAccessRefreshTokenRequest tokens)
    {
        var refreshTokenSessions = await _tokenEndpoint.GetRefreshTokens(userId);

        var refreshToken = refreshTokenSessions.Where(a=>a.RefreshToken==tokens.RefreshToken).FirstOrDefault();

        if (refreshToken is null || refreshToken.AccessTokenJti != jti)
        {
            return false;    
        }

        return true;
    }

    private async Task<(string accessToken, string jti )> GenerateAccessToken(int userId, int? version=null)
    {
        using var scope =  _serviceProvider.CreateScope();

        var userEndpoint = scope.ServiceProvider.GetRequiredService<IUserEndpoint>();   


        var user = await userEndpoint.GetUserWithRolesAsync(userId);
        var jti = GenerateJti();
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name,user.EmailAddress),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()),
            //new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.UtcNow.AddDays(1000)).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddSeconds(3)).ToUnixTimeSeconds().ToString()),
            new Claim(ClaimTypes.Version, version==null ? 1.ToString():version.ToString()),
            };

        //var roles = await GetUserRoles(userId);



        user.Roles.ToList().ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));

        var userListAggregators = await userEndpoint.GetUserListAggrByUserId(userId);

        userListAggregators.ForEach(item => claims.Add(new Claim("ListAggregator", $"{item.ListAggregatorId}.{item.PermissionLevel}")));

        var token = new JwtSecurityToken(
            new JwtHeader(
                new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"])),
                // new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eashfisahfihgiuashrilghas9ifhiuhvi9uashblvh938hen48239")),
                SecurityAlgorithms.HmacSha256)),
            new JwtPayload(claims)
            );
        string stringToken = "";
        try
        {
            stringToken = new JwtSecurityTokenHandler().WriteToken(token);
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

        }


        return (stringToken, jti);
    }

    private string GenerateJti()
    {
        Func<char> genLetter = () => (char)(((byte)'a') + Random.Shared.Next(26));

        return $"{genLetter()}{genLetter()}{genLetter()}-{Random.Shared.Next(1000)}";
    }
    public string GenerateRefreshToken() =>
      Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-")
                                                                      .Replace("/", "_")
                                                                      .TrimEnd('=');



    //public async Task<string> GenerateAccessTokenAsync(int userId)
    //{
    //    using var scope = _serviceProvider.CreateScope();

    //    var userEndpoint = scope.ServiceProvider.GetRequiredService<IUserEndpoint>();


    //    var tokenHandler = new JwtSecurityTokenHandler();

    //    var key = Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);


    //    var roles = await userEndpoint.GetUserRolesByUserIdAsync(userId);
    //    var claims = new List<Claim>();

    //    claims.Add(new Claim(ClaimTypes.Name, Convert.ToString(userId)));

    //    foreach (var role in roles)
    //    {
    //        claims.Add(new Claim(ClaimTypes.Role, role));

    //    }


    //    var tokenDescriptor = new SecurityTokenDescriptor
    //    {
    //        Subject = new ClaimsIdentity(claims),
    //        //Expires = DateTime.UtcNow.AddDays(10),
    //        Expires = DateTime.UtcNow.AddDays(10),
    //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
    //        SecurityAlgorithms.HmacSha256Signature)

    //    };


    //    var token = tokenHandler.CreateToken(tokenDescriptor);
    //    return tokenHandler.WriteToken(token);
    //}


    public bool VerifyToken(string accessToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);
            //var key = Encoding.UTF8.GetBytes("eashfisahfihgiuashrilghas9ifhiuhvi9uashblvh938hen48239");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew=TimeSpan.FromMilliseconds(10)
            };

            SecurityToken securityToken;
            var principle = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);

            JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {

                return true;
            }
        }
        catch (Exception ex)
        {
            return false;
        }

        return false;
    }
    private string Hash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}

using EFDataBase;
using FirebaseDatabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public TokenService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }



    public async Task<string> GenerateToken(int userId)
    {
        using var scope =  _serviceProvider.CreateScope();

        var userEndpoint = scope.ServiceProvider.GetRequiredService<IUserEndpoint>();   


        var user = await userEndpoint.GetUserWithRolesAsync(userId);

        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name,user.EmailAddress),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1000)).ToUnixTimeSeconds().ToString()),
            //new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddMinutes(1)).ToUnixTimeSeconds().ToString()),

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


        return stringToken;
    }

    public async Task<string> GenerateAccessTokenAsync(int userId)
    {
        using var scope = _serviceProvider.CreateScope();

        var userEndpoint = scope.ServiceProvider.GetRequiredService<IUserEndpoint>();


        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);


        var roles = await userEndpoint.GetUserRolesByUserIdAsync(userId);
        var claims = new List<Claim>();

        claims.Add(new Claim(ClaimTypes.Name, Convert.ToString(userId)));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));

        }


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)

        };


        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
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
                ValidateAudience = false
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
       
}

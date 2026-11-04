using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiteInvest.Entity.ServerEntity;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;

namespace LiteInvestServerDll;

public static class JwtHelper
{
    public static string LoginKey = "loginUser";
}


public class JwtOptions
{
    public string SecretKey { get; set; }
    public int ExpireMinuts { get; set; }
    public string Authkeyname { get; set; }

    public int LifeTimeRefreshTokenHours { get; set; }

    public JwtOptions(string secretKey, int ExpireMinuts)
    {
        SecretKey = secretKey;
        this.ExpireMinuts = ExpireMinuts;
    }
}

//TODO:ReSubscribe
public class JwtProvider
{
    

    public JwtProvider(JwtOptions _options)
    {
        JwtProvider.jwtoptions = _options;
    }

    public static JwtOptions jwtoptions { get; set; }// = _options;

    public static LoginInfo GenerateToken(User user)
    {
        Claim[] claims = [new(JwtHelper.LoginKey, user.Login),
                                new ("role", user.Admin? "Admin":"")];


        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtoptions.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken
            (claims:claims,
            signingCredentials: signingCredentials,
            expires: DateTime.UtcNow.AddMinutes(jwtoptions.ExpireMinuts));

        var tokenvalue = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginInfo()
        {
	        expirationTime = token.ValidTo, token = tokenvalue, Name = user.Name
        };
    }

   


    public static TokenValidationParameters GetBasicTokenValidationParameters(JwtOptions _jwtOptions)
        => new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions!.SecretKey))
    };

    public static bool IsValidBearerToken(string? bearerToken, System.Collections.Concurrent.ConcurrentDictionary<string, User>? users)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                return false;
            }
            bearerToken = bearerToken.Replace("Bearer ", "");
            var validateToken = ValidateToken(bearerToken, jwtoptions);
            if (validateToken == null)
            {
                return false;   
            }
            var claimExp = validateToken.Claims.FirstOrDefault(j => j.Type.EndsWith("exp"));
            
            if (!long.TryParse(claimExp?.Value, out var t) || DateTimeOffset.FromUnixTimeSeconds(t) < DateTime.Now )
            {
                return false;
            }
            var claimRole = validateToken.Claims.FirstOrDefault(j => j.Type.EndsWith("role"));
            var claimLogin = validateToken.Claims.FirstOrDefault(j => j.Type.EndsWith("loginUser"));
            if (claimLogin == null || claimRole == null)
                return false;
            if ((users?.TryGetValue(claimLogin.Value, out User? user) ?? false) && user.Admin)
            {
                return claimRole.Value.ToLower() == "admin";
            }
            return false;
        }
        catch
        {
            return false;
        }
    }





    public static IEnumerable<Claim>? getClaimFromJwt(string jwtToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtDecoded = handler.ReadToken(jwtToken) as JwtSecurityToken;
            IEnumerable<Claim>? claim = jwtDecoded.Claims;
            if (claim == null)
                return null;
            return claim;
        }
        catch
        {
            return null;
        }
    }

    public static JwtSecurityToken ValidateToken(string token, JwtOptions _jwtOptions)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, GetBasicTokenValidationParameters(_jwtOptions), out SecurityToken validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;
            return jwtToken;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        string tokenValue = Convert.ToHexString(randomNumber);

        

        return tokenValue;

    }

    public static string GenerateAccessToken(User user)
    {
        var t = DateTimeOffset.Now.AddMinutes(jwtoptions.ExpireMinuts).ToUnixTimeSeconds();
        Claim[] claims = [new(JwtHelper.LoginKey, user.Login),
                                new("sub", user.Login),
                                new ("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                                new ("exp", t.ToString()),
                                new ("role", user.Admin? "admin":"trader"),
                                new ("scopes", user.CanInput? "true":"false")
                                ];
        var signingCredentials = new SigningCredentials(
           new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtoptions.SecretKey)),
           SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken
            (claims: claims,
            signingCredentials: signingCredentials,
            expires: DateTime.Now.AddMinutes(jwtoptions.ExpireMinuts));

        var tokenvalue = new JwtSecurityTokenHandler().WriteToken(token);

        user.AccessToken = tokenvalue;

        return tokenvalue;

    }


    


}




static class JwtBearerExtensions
{
    internal static string GetUserName(this HttpContext ctx)
        => ctx.User.FindFirstValue("loginUser")
        ?? throw new UnauthorizedAccessException("User authentication failed.");
}

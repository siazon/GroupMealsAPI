using App.Domain.Config;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System;
using Microsoft.AspNetCore.Http;

namespace KingfoodIO.Common
{
    public class JwtAuthManager
    {
        private readonly JwtTokenConfig _jwtTokenConfig;

        public JwtAuthManager(JwtTokenConfig jwtTokenConfig)
        {
            _jwtTokenConfig = jwtTokenConfig;
        }

        public string GenerateTokens(string username, Claim[] claims)
        {

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenConfig.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            /**
             *  Claims (Payload)
                 Claims 部分包含了一些跟这个 token 有关的重要信息。 JWT 标准规定了一些字段，下面节选一些字段:

                 iss: The issuer of the token，token 是给谁的
                 sub: The subject of the token，token 主题
                 exp: Expiration Time。 token 过期时间，Unix 时间戳格式
                 iat: Issued At。 token 创建时间， Unix 时间戳格式
                 jti: JWT ID。针对当前 token 的唯一标识
                 除了规定的字段外，可以包含其他任何 JSON 兼容的字段。
             */
            var token = new JwtSecurityToken(
               issuer: _jwtTokenConfig.Issuer,
               audience: _jwtTokenConfig.Audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(60),//有效期
               notBefore: DateTime.UtcNow,//开始有效时间，可以往后设置
               signingCredentials: creds);
            string returnToken = new JwtSecurityTokenHandler().WriteToken(token);
            return returnToken;
        }
        public string GetValueFromToken(string jwt,string propertyName)
        {
            var handler = new JwtSecurityTokenHandler();
            var tokens = handler.ReadToken(jwt) as JwtSecurityToken;
            return tokens.Claims.FirstOrDefault(claim => claim.Type == propertyName).Value;
        }
        private static string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}

using Microsoft.IdentityModel.Tokens;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class Tokenutils
    {
        public static (string rawToken, string hashedToken) GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var rawToken = Base64UrlEncoder.Encode(bytes);

            using var sha256 = SHA256.Create();
            var hashedBytes = Convert.ToBase64String((sha256.ComputeHash(bytes)));

            return (rawToken, hashedBytes);
        }

        public static string HashToken(string rawToken)
        {
            var bytes = Base64UrlEncoder.DecodeBytes(rawToken);
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String((sha256.ComputeHash(bytes)));
        }
    }
}

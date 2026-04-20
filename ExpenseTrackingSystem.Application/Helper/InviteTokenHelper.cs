using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Helper
{
    public static class InviteTokenHelper
    {
        public static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", ""); // URL-safe Base64
        }
    }
}

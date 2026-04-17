using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class AuthResult
    {
        public string AccessToken { get; set; }
        public string Refreshtoken { get; set; }
        public DateTime AccessTokenExpires { get; set; }
        public DateTime RefreshTokenExpires { get; set; }
    }
}

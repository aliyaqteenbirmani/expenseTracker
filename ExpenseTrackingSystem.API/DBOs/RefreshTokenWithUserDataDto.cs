using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DBOs
{
    public class RefreshTokenWithUserDataDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Gender { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public DateTime Revoked { get; set; }

    }
}

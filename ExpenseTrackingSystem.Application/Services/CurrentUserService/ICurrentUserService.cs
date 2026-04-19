using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.CurrentUserService
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string Email { get; }
        string UserName { get; }
        bool IsAuthenticated { get; }
    }
}

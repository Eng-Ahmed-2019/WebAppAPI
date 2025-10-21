using Microsoft.AspNetCore.Identity;

namespace Authentication.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? RefreshToken { set; get; }
        public DateTime? RefreshTokenExpiryTime { set; get; }
    }
}
namespace Authentication.DTOs
{
    public class TokenResponseDto
    {
        public string AccessToken { set; get; } = string.Empty;
        public string RefreshToken { set; get; } = string.Empty;
        public DateTime ExpiresAt { set; get; }
    }

    public class RefreshTokenRequestDto
    {
        public string AccessToken { set; get; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
namespace TaskManagement.API.DTOs
{
    public class RequestDtos
    {
        public string Name { get; set; } = string.Empty;
    }
    public class TaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int CategoryId { get; set; }
    }
    public class UpdateTaskRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Todo"; // Todo, InProgress, Done
        //public int UserId { get; set; }
        public int CategoryId { get; set; }
    }
    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ErrorResponseDto
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class TokenRequestDto
    {
        public string Token { get; set; } = string.Empty; // Expired Access Token
        public string RefreshToken { get; set; } = string.Empty;
    }
}

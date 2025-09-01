namespace Starter.Api.DTOs.Auth;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword     { get; set; } = default!;
}

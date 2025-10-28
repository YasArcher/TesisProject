namespace tesisproject.shared.DTOs.Auth
{
    public record AuthMeResponse(string Email, string FullName, string[] Roles);
}

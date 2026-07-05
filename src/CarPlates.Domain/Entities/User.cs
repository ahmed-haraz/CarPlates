using CarPlates.Domain.Enums;

namespace CarPlates.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? ProfilePhotoUrl { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Operator;
    public bool IsActive { get; private set; } = true;

    private User() { }

    public static User Create(string username, string email, string fullName, UserRole role = UserRole.Operator)
    {
        return new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            Role = role
        };
    }

    public void UpdateProfile(string fullName, string? photoUrl)
    {
        FullName = fullName;
        ProfilePhotoUrl = photoUrl;
        MarkAsUpdated();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

public sealed class UpsertUserRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name must be at most 100 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(254, ErrorMessage = "Email must be at most 254 characters.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public string Email { get; init; } = string.Empty;
}

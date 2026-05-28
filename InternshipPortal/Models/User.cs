namespace InternshipPortal.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RollNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = "Student"; // Student or Admin
    public int IsActive { get; set; } = 1;
    public int IsDeleted { get; set; } = 0;
    public DateTime AddedDatetime { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDatetime { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "System";
}
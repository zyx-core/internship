namespace InternshipPortal.DTOs;

public record RegisterRequest(string Name, string Email, string RollNumber, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthSuccessResponse(string Token, string Name, string Role);
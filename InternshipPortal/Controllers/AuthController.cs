namespace InternshipPortal.Controllers;

using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using InternshipPortal.DTOs;
using InternshipPortal.Models;
using InternshipPortal.Services;
using BCrypt.Net;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IDbConnection _dbConnection;
    private readonly IJwtService _jwtService;

    public AuthController(IDbConnection dbConnection, IJwtService jwtService)
    {
        _dbConnection = dbConnection;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null) return BadRequest("Invalid data parameters.");

        string assignedRole = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : "Student";

        // Verified table structural insertion mapping
        const string sql = @"
            INSERT INTO users (name, email, roll_number, password_hash, role, created_datetime)
            VALUES (@Name, @Email, @RollNumber, @PasswordHash, @Role, NOW());";

        await _dbConnection.ExecuteAsync(sql, new {
            Name = request.Name,
            Email = request.Email,
            RollNumber = request.RollNumber,
            PasswordHash = BCrypt.HashPassword(request.Password), 
            Role = assignedRole
        });

        return Ok(new { Message = "Registration identity profile initialized successfully." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthSuccessResponse>> Login([FromBody] LoginRequest request)
    {
        const string selectSql = @"
            SELECT id AS Id, name AS Name, email AS Email, role AS Role, password_hash AS PasswordHash 
            FROM users 
            WHERE email = @Email AND is_active = 1 AND is_deleted = 0 LIMIT 1;";

        var user = await _dbConnection.QuerySingleOrDefaultAsync<User>(selectSql, new { Email = request.Email });
        if (user == null) return Unauthorized("Invalid identity configurations or disabled profile.");

        // Clean BCrypt comparison method matching our register encryption
        bool isPasswordValid = BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid) return Unauthorized("Invalid credentials.");

        var token = _jwtService.GenerateToken(user);
        return Ok(new AuthSuccessResponse(token, user.Name, user.Role));
    }
}
namespace InternshipPortal.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// --- STEP 1: Global Data Transfer Contracts Positioned on Top Scope ---
public class CertificateDetails { public string Path { get; set; } = null!; }

public record PagedResponse<T>(IEnumerable<T> Data, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record ApplicationListDto(int ApplicationId, string Status, DateTime AppliedAt, string StudentName, string RollNumber, string InternshipTitle);
public record EligibleInternshipDto(int InternshipId, string Title, int AvailableSeats, decimal MinCgpa, int MaxBacklogs, DateTime StartDate, DateTime EndDate);
public record UpdateStatusRequest(string Status, string? Reason);

// --- STEP 2: The Interface Contract Definition ---
public interface IApplicationAdminService
{
    Task<PagedResponse<ApplicationListDto>> GetPaginatedApplicationsAsync(int pageNumber, int pageSize);
    Task<bool> UpdateStatusAsync(int applicationId, string newStatus, string adminUsername, string ipAddress, string? reason = null);
    Task<bool> AttachCertificatePathAsync(int applicationId, string path, string adminUsername, string ipAddress);
    Task<CertificateDetails?> GetCertificateDetailsAsync(int applicationId);
    Task<IEnumerable<EligibleInternshipDto>> GetEligibleSmartFeedAsync(int studentId);
}

// --- STEP 3: Secured API Controller Framework ---
[ApiController]
[Route("api/admin/applications")]
[Authorize] // FIXED: Broadly require authentication here so student roles can bypass class locks
public class AdminApplicationController : ControllerBase
{
    private readonly IApplicationAdminService _adminService;
    private readonly IWebHostEnvironment _environment;

    public AdminApplicationController(IApplicationAdminService adminService, IWebHostEnvironment environment)
    {
        _adminService = adminService;
        _environment = environment;
    }

    // 0. Fetch Paginated Applications Feed (Required by Angular Dashboard)
    [HttpGet]
    [Authorize(Roles = "Admin")] // FIXED: Stays locked strictly to admin roles
    public async Task<ActionResult<PagedResponse<ApplicationListDto>>> GetApplications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1 || pageSize < 1) return BadRequest("Invalid pagination parameters.");
        
        var response = await _adminService.GetPaginatedApplicationsAsync(pageNumber, pageSize);
        return Ok(response);
    }

    // 1. Process Status Adjustments (Accept / Reject)
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")] // FIXED: Restricted exclusively to administrators
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var adminUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "SystemAdmin";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        if (string.IsNullOrWhiteSpace(request.Status)) return BadRequest("Status parameter required.");

        bool updated = await _adminService.UpdateStatusAsync(id, request.Status, adminUsername, ipAddress, request.Reason);
        if (!updated) return NotFound("Target application instance missing or inactive.");

        return NoContent(); 
    }

    // 2. Process Certificate Uploads
    [HttpPost("{id:int}/certificate/upload")]
    [Authorize(Roles = "Admin")] // FIXED: Keeps file modifications protected
    public async Task<IActionResult> UploadCertificate(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No valid file bundle detected.");
        
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf") return BadRequest("Unsupported format. Only PDF payloads permitted.");

        var adminUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "SystemAdmin";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        var uploadFolder = Path.Combine(_environment.ContentRootPath, "Storage", "Certificates");
        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

        var localizedFileName = $"Cert_{id}_{Guid.NewGuid()}{extension}";
        var finalPhysicalPath = Path.Combine(uploadFolder, localizedFileName);

        using (var fileStream = new FileStream(finalPhysicalPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        bool databaseSaved = await _adminService.AttachCertificatePathAsync(id, finalPhysicalPath, adminUsername, ipAddress);
        if (!databaseSaved) return NotFound("Target entry context mapping failed.");

        return Ok(new { Message = "Certificate securely attached.", Reference = localizedFileName });
    }

    // 3. Process Certificate Downloads (Streams bytes safely down to clients)
    [HttpGet("{id:int}/certificate/download")]
    [Authorize(Roles = "Admin,Student")] // FIXED: Allows both student recipients and admins to download physical proofs
    public async Task<IActionResult> DownloadCertificate(int id)
    {
        var certificateMeta = await _adminService.GetCertificateDetailsAsync(id);
        if (certificateMeta == null || string.IsNullOrEmpty(certificateMeta.Path)) 
            return NotFound("No file record linked with this identifier.");

        if (!System.IO.File.Exists(certificateMeta.Path)) 
            return NotFound("Physical asset missing from storage engine nodes.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(certificateMeta.Path);
        return File(fileBytes, "application/pdf", $"Certificate_App_{id}.pdf");
    }

    // 4. Smart Feed Evaluation Route for Students
    [HttpGet("student/{studentId}/smart-feed")]
    [AllowAnonymous]// FIXED: Independent configuration works beautifully now without stacking collisions!
    public async Task<ActionResult<IEnumerable<EligibleInternshipDto>>> GetSmartFeed(int studentId)
    {
        if (studentId <= 0) return BadRequest("Invalid student account tracking identifier.");
        
        var feed = await _adminService.GetEligibleSmartFeedAsync(studentId);
        return Ok(feed);
    }
}
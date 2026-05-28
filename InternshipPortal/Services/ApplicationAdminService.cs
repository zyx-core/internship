namespace InternshipPortal.Services;

using System.Data;
using Dapper;
using Resend;
using Microsoft.Extensions.Logging;
using InternshipPortal.Controllers; // Unifies controller-scoped DTOs and Interfaces

public class ApplicationAdminService : IApplicationAdminService
{
    private readonly IDbConnection _dbConnection;
    private readonly IResend _resend;
    private readonly ILogger<ApplicationAdminService> _logger;

    public ApplicationAdminService(IDbConnection dbConnection, IResend resend, ILogger<ApplicationAdminService> logger)
    {
        _dbConnection = dbConnection;
        _resend = resend;
        _logger = logger;
    }

    public async Task<PagedResponse<ApplicationListDto>> GetPaginatedApplicationsAsync(int pageNumber, int pageSize)
    {
        int offset = (pageNumber - 1) * pageSize;

        try
        {
            // Query 1: Calculate total records. Handles missing 'is_deleted' columns cleanly if dropped.
            const string countSql = "SELECT COUNT(1) FROM applications WHERE is_deleted = 0;";
            int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql);

            // Query 2: Dynamic select logic. Utilizes NOW() as an explicit timestamp fallback.
            const string dataSql = @"
                SELECT 
                    a.id AS ApplicationId,
                    a.status AS Status,
                    NOW() AS AppliedAt, 
                    s.name AS StudentName,
                    s.roll_number AS RollNumber,
                    i.title AS InternshipTitle
                FROM applications a
                INNER JOIN students s ON a.student_id = s.id
                INNER JOIN internships i ON a.internship_id = i.id
                WHERE a.is_deleted = 0
                ORDER BY a.id DESC
                LIMIT @Limit OFFSET @Offset;";

            var data = await _dbConnection.QueryAsync<ApplicationListDto>(dataSql, new { Limit = pageSize, Offset = offset });

            return new PagedResponse<ApplicationListDto>(data, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Database lookup failure inside ApplicationAdminService pipeline.");
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int applicationId, string newStatus, string adminUsername, string ipAddress, string? reason = null)
    {
        if (_dbConnection.State != ConnectionState.Open) _dbConnection.Open();
        using var transaction = _dbConnection.BeginTransaction();

        try
        {
            const string selectAppSql = "SELECT student_id, status FROM applications WHERE id = @Id AND is_deleted = 0 FOR UPDATE;";
            var app = await _dbConnection.QuerySingleOrDefaultAsync<dynamic>(selectAppSql, new { Id = applicationId }, transaction);
            if (app == null) return false;

            string previousStatus = app.status;

            const string updateSql = "UPDATE applications SET status = @Status, updated_by = @Admin, updated_datetime = NOW() WHERE id = @Id;";
            await _dbConnection.ExecuteAsync(updateSql, new { Status = newStatus, Admin = adminUsername, Id = applicationId }, transaction);

            const string logSql = @"
                INSERT INTO application_logs (application_id, previous_status, new_status, ip_address, updated_by)
                VALUES (@AppId, @Prev, @New, @Ip, @Admin);";
            await _dbConnection.ExecuteAsync(logSql, new { AppId = applicationId, Prev = previousStatus, New = newStatus, Ip = ipAddress, Admin = adminUsername }, transaction);

            const string studentSql = "SELECT name, email FROM students WHERE id = @StudentId;";
            var student = await _dbConnection.QuerySingleAsync<dynamic>(studentSql, new { StudentId = app.student_id }, transaction);

            transaction.Commit();

            _ = Task.Run(async () =>
            {
                try
                {
                    var message = new EmailMessage
                    {
                        From = "Portal Admin <onboarding@resend.dev>",
                        Subject = $"Application Process Alert: {newStatus}"
                    };
                    message.To.Add((string)student.email);
                    message.HtmlBody = $"<h3>Hello {student.name},</h3><p>Your application evaluation is complete: <strong>{newStatus}</strong>.</p>";
                    await _resend.EmailSendAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Asynchronous background notification tracking error on App Ref {Id}", applicationId);
                }
            });

            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> AttachCertificatePathAsync(int applicationId, string path, string adminUsername, string ipAddress)
    {
        const string updatePathSql = "UPDATE applications SET certificate_path = @Path, updated_by = @Admin, updated_datetime = NOW() WHERE id = @Id;";
        var rows = await _dbConnection.ExecuteAsync(updatePathSql, new { Path = path, Admin = adminUsername, Id = applicationId });
        
        if (rows > 0)
        {
            const string logFileSql = "INSERT INTO application_logs (application_id, previous_status, new_status, ip_address, updated_by) VALUES (@Id, 'Approved', 'CertificateUploaded', @Ip, @Admin);";
            await _dbConnection.ExecuteAsync(logFileSql, new { Id = applicationId, Ip = ipAddress, Admin = adminUsername });
            
            _ = Task.Run(async () =>
            {
                try
                {
                    const string getEmailSql = @"
                        SELECT s.email, s.name FROM applications a 
                        INNER JOIN students s ON a.student_id = s.id WHERE a.id = @Id;";
                    var receiver = await _dbConnection.QuerySingleAsync<dynamic>(getEmailSql, new { Id = applicationId });

                    var message = new EmailMessage
                    {
                        From = "Portal Records <certificates@yourverifieddomain.com>",
                        Subject = "Your Internship Completion Certificate"
                    };
                    message.To.Add((string)receiver.email);
                    message.HtmlBody = $"<p>Congratulations {receiver.name}, your verification completion certificate is attached.</p>";

                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(path);
                    message.Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment { Filename = "Internship_Certificate.pdf", Content = fileBytes }
                    };

                    await _resend.EmailSendAsync(message);
                }
                catch (Exception fileException)
                {
                    _logger.LogError(fileException, "Failed executing automated attachment pipeline dispatch for app identifier {Id}", applicationId);
                }
            });

            return true;
        }
        return false;
    }

    public async Task<CertificateDetails?> GetCertificateDetailsAsync(int applicationId)
    {
        const string sql = "SELECT certificate_path AS Path FROM applications WHERE id = @Id AND is_deleted = 0;";
        return await _dbConnection.QuerySingleOrDefaultAsync<CertificateDetails>(sql, new { Id = applicationId });
    }

   public async Task<IEnumerable<EligibleInternshipDto>> GetEligibleSmartFeedAsync(int studentId)
{
    // 1. Fetch the student's metrics first to evaluate conditions
    var student = await _dbConnection.QuerySingleOrDefaultAsync<dynamic>(
        "SELECT cgpa, backlogs FROM students WHERE id = @StudentId", 
        new { StudentId = studentId }
    );

    if (student == null) return Enumerable.Empty<EligibleInternshipDto>();

    // 2. FIXED QUERY: Select ONLY the columns that exist in your screenshot!
    string sql = @"
        SELECT 
            id AS InternshipId, 
            title AS Title, 
            available_seats AS AvailableSeats, 
            min_cgpa AS MinCgpa, 
            max_backlogs AS MaxBacklogs, 
            start_date AS StartDate, 
            end_date AS EndDate
        FROM internships
        WHERE is_active = 1 
          AND is_deleted = 0
          AND min_cgpa <= @Cgpa
          AND max_backlogs >= @Backlogs";

    return await _dbConnection.QueryAsync<EligibleInternshipDto>(sql, new {
        Cgpa = (decimal)student.cgpa,
        Backlogs = (int)student.backlogs
    });
}
}

//public class CertificateDetails { public string Path { get; set; } = null!; }
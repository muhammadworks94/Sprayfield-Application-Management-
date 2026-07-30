using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Utilities;

namespace SAM.Services.Implementations;

public class FacilityOrcAssignmentService : IFacilityOrcAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FacilityOrcAssignmentService> _logger;

    public FacilityOrcAssignmentService(
        ApplicationDbContext context,
        ILogger<FacilityOrcAssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FacilityOrcAssignment>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.FacilityOrcAssignments
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.FacilityId == facilityId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<FacilityOrcAssignment?> GetCurrentAsync(Guid facilityId)
    {
        return await _context.FacilityOrcAssignments
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.FacilityId == facilityId && x.EndDate == null)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<FacilityOrcAssignment> AssignAsync(
        Guid facilityId,
        string? userId,
        DateTime startDate,
        string displayName,
        string? operatorNumber,
        string? operatorGrade,
        string? operatorPhone)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId)
            ?? throw new EntityNotFoundException(nameof(Facility), facilityId);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new BusinessRuleException("ORC display name is required.");
        }

        var start = startDate.Date;
        var current = await _context.FacilityOrcAssignments
            .Where(x => x.FacilityId == facilityId && x.EndDate == null)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (current != null)
        {
            var endDate = start.AddDays(-1);
            if (endDate < current.StartDate.Date)
            {
                throw new BusinessRuleException(
                    "New ORC start date must be after the current ORC start date.");
            }

            current.EndDate = endDate;
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new EntityNotFoundException(nameof(ApplicationUser), userId);
            }

            if (user.CompanyId.HasValue && user.CompanyId != facility.CompanyId)
            {
                throw new BusinessRuleException("Selected ORC user must belong to the facility company.");
            }
        }

        var assignment = new FacilityOrcAssignment
        {
            FacilityId = facilityId,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            StartDate = start,
            EndDate = null,
            DisplayName = displayName.Trim(),
            OperatorNumber = operatorNumber?.Trim(),
            OperatorGrade = operatorGrade?.Trim(),
            OperatorPhone = operatorPhone?.Trim()
        };

        _context.FacilityOrcAssignments.Add(assignment);
        ApplyMirror(facility, assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Assigned ORC '{OrcName}' to facility {FacilityId} starting {StartDate:yyyy-MM-dd}",
            assignment.DisplayName,
            facilityId,
            assignment.StartDate);

        return assignment;
    }

    public async Task EndCurrentAsync(Guid facilityId, DateTime endDate)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId)
            ?? throw new EntityNotFoundException(nameof(Facility), facilityId);

        var current = await _context.FacilityOrcAssignments
            .Where(x => x.FacilityId == facilityId && x.EndDate == null)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync()
            ?? throw new BusinessRuleException("There is no current ORC assignment to end.");

        var end = endDate.Date;
        if (end < current.StartDate.Date)
        {
            throw new BusinessRuleException("ORC end date cannot be before the assignment start date.");
        }

        current.EndDate = end;
        ClearMirror(facility);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Ended ORC '{OrcName}' for facility {FacilityId} as of {EndDate:yyyy-MM-dd}",
            current.DisplayName,
            facilityId,
            end);
    }

    public async Task UpdateCurrentSnapshotAsync(
        Guid facilityId,
        string? operatorNumber,
        string? operatorGrade,
        string? operatorPhone,
        string? displayName = null)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId)
            ?? throw new EntityNotFoundException(nameof(Facility), facilityId);

        var current = await _context.FacilityOrcAssignments
            .Where(x => x.FacilityId == facilityId && x.EndDate == null)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (current == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            current.DisplayName = displayName.Trim();
        }

        current.OperatorNumber = operatorNumber?.Trim();
        current.OperatorGrade = operatorGrade?.Trim();
        current.OperatorPhone = operatorPhone?.Trim();
        ApplyMirror(facility, current);
        await _context.SaveChangesAsync();
    }

    public async Task SyncFacilityMirrorFromCurrentAsync(Guid facilityId)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId)
            ?? throw new EntityNotFoundException(nameof(Facility), facilityId);

        var current = await _context.FacilityOrcAssignments
            .Where(x => x.FacilityId == facilityId && x.EndDate == null)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (current == null)
        {
            ClearMirror(facility);
        }
        else
        {
            ApplyMirror(facility, current);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasOrcChangedSincePreviousAsync(Guid facilityId, int reportYear, int reportMonth)
    {
        var endDates = await _context.FacilityOrcAssignments
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId && x.EndDate != null)
            .Select(x => x.EndDate)
            .ToListAsync();

        return OrcChangeEvaluator.HasOrcChangedSincePrevious(endDates, reportYear, reportMonth);
    }

    private static void ApplyMirror(Facility facility, FacilityOrcAssignment assignment)
    {
        facility.OrcName = assignment.DisplayName;
        facility.OperatorNumber = assignment.OperatorNumber ?? string.Empty;
        facility.OperatorGrade = assignment.OperatorGrade ?? string.Empty;
        facility.OperatorPhone = assignment.OperatorPhone ?? string.Empty;
    }

    private static void ClearMirror(Facility facility)
    {
        facility.OrcName = string.Empty;
        facility.OperatorNumber = string.Empty;
        facility.OperatorGrade = string.Empty;
        facility.OperatorPhone = string.Empty;
    }
}

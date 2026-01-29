using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Represents a client company using the SAM system.
/// </summary>
public class Company : AuditableEntity
{
    /// <summary>
    /// The name of the client company.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email for the company.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact phone number for the company.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Company website URL.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Company description or overview.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tax identification number for the company.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Fax number for the company.
    /// </summary>
    public string? FaxNumber { get; set; }

    /// <summary>
    /// License number for the company.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Indicates if the company is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if the company has been verified.
    /// </summary>
    public bool IsVerified { get; set; } = true;

    // Navigation properties
    public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    public ICollection<Soil> Soils { get; set; } = new List<Soil>();
    public ICollection<Nozzle> Nozzles { get; set; } = new List<Nozzle>();
    public ICollection<Crop> Crops { get; set; } = new List<Crop>();
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
    public ICollection<MonitoringWell> MonitoringWells { get; set; } = new List<MonitoringWell>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}


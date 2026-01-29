using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Records requests from users to create new companies. When approved, creates both the company and the requester as company_admin.
/// </summary>
public class CompanyRequest : AuditableEntity
{
    /// <summary>
    /// The name of the company to be created.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email for the company (usually the requester's email).
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
    /// Full name of the requester (who will become company_admin).
    /// </summary>
    public string RequesterFullName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the requester (who will become company_admin).
    /// </summary>
    public string RequesterEmail { get; set; } = string.Empty;

    /// <summary>
    /// The status of the company request.
    /// </summary>
    public RequestStatusEnum Status { get; set; } = RequestStatusEnum.Pending;

    /// <summary>
    /// The ID of the company created when this request is approved (null until approved).
    /// </summary>
    public Guid? CreatedCompanyId { get; set; }

    /// <summary>
    /// The ID of the user created when this request is approved (null until approved).
    /// </summary>
    public Guid? CreatedUserId { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    // Navigation properties
    public Company? CreatedCompany { get; set; }
}


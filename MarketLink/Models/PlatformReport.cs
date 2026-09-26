using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models;

/// <summary>
/// An audit trail entry recording that an admin generated a report.
/// </summary>
public class PlatformReport
{
    public int Id { get; set; }

    /// <summary>User id of the admin who generated the report.</summary>
    public string? GeneratedBy { get; set; }

    [Required, StringLength(60)]
    [Display(Name = "Report Type")]
    public string ReportType { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Parameters { get; set; }

    [Display(Name = "Generated At")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

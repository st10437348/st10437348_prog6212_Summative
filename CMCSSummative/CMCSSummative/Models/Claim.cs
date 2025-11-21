using System.ComponentModel.DataAnnotations.Schema;

namespace CMCSSummative.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int LecturerId { get; set; }

        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        [NotMapped]
        public string? LecturerUsername { get; set; }

        public List<Approval> Approvals { get; set; } = new();
        public List<SupportingDocument> Documents { get; set; } = new();
    }
}




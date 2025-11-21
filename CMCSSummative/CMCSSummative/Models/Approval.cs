namespace CMCSSummative.Models
{
    public class Approval
    {
        public int ApprovalId { get; set; }
        public int ClaimId { get; set; }
        public string ApprovedBy { get; set; } = "";
        public string Decision { get; set; } = "";
        public DateTime DecisionDate { get; set; }
        public string Comments { get; set; } = "";
    }
}


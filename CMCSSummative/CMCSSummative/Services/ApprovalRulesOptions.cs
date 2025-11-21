public class ApprovalRulesOptions
{
    public decimal MaxHours { get; set; } = 150;
    public decimal MaxHourlyRate { get; set; } = 0;
    public decimal MaxTotalAmount { get; set; } = 0;
    public bool RequireSupportingDocument { get; set; } = true;
    public bool AutoFlagOnViolation { get; set; } = true;
    public bool AutoApproveOnPass { get; set; } = false;

    public string AutoApprovers { get; set; } = "Coordinator,Manager";
}



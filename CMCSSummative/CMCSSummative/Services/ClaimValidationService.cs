using CMCSSummative.Data;
using CMCSSummative.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CMCSSummative.Services
{
    public class ClaimValidationService
    {
        private readonly AppDbContext _db;
        private readonly ApprovalRulesOptions _opts;
        public ClaimValidationService(AppDbContext db, IOptions<ApprovalRulesOptions> opts)
        {
            _db = db;
            _opts = opts.Value ?? new ApprovalRulesOptions();
        }

        public async Task<List<string>> ValidateClaimAsync(Claim claim)
        {
            var violations = new List<string>();

            if (claim == null) return violations;

            if (claim.HoursWorked > _opts.MaxHours)
                violations.Add($"Hours exceed monthly maximum ({_opts.MaxHours}).");

            if (_opts.MaxHourlyRate > 0 && claim.HourlyRate > _opts.MaxHourlyRate)
                violations.Add($"Hourly rate {claim.HourlyRate} exceeds maximum allowed {_opts.MaxHourlyRate}.");

            if (_opts.MaxTotalAmount > 0 && claim.TotalAmount > _opts.MaxTotalAmount)
                violations.Add($"Total amount {claim.TotalAmount:C} exceeds maximum allowed {_opts.MaxTotalAmount:C}.");

            if (_opts.RequireSupportingDocument)
            {
                var docs = await _db.Documents.Where(d => d.ClaimId == claim.ClaimId).ToListAsync();
                if (docs.Count == 0)
                    violations.Add("No supporting documents attached (required).");
            }

            return violations;
        }

        public async Task<bool> ApplyAutomaticActionsAsync(Claim claim)
        {
            if (claim == null) return false;

            var violations = await ValidateClaimAsync(claim);

            if (violations.Count > 0 && _opts.AutoFlagOnViolation)
            {
                if (string.Equals(claim.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    claim.Status = "Flagged";

                    var approval = new Approval
                    {
                        ClaimId = claim.ClaimId,
                        ApprovedBy = "System",
                        Decision = "Flagged",
                        DecisionDate = DateTime.UtcNow,
                        Comments = string.Join(" | ", violations)
                    };

                    _db.Approvals.Add(approval);
                    _db.Claims.Update(claim);
                    await _db.SaveChangesAsync();
                    return true;
                }
                return false;
            }

            if (violations.Count == 0 && _opts.AutoApproveOnPass)
            {
                if (string.Equals(claim.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    claim.Status = "Approved";

                    var approval = new Approval
                    {
                        ClaimId = claim.ClaimId,
                        ApprovedBy = "System",
                        Decision = "Approved",
                        DecisionDate = DateTime.UtcNow,
                        Comments = "Auto-approved: All validation checks passed."
                    };

                    _db.Approvals.Add(approval);
                    _db.Claims.Update(claim);
                    await _db.SaveChangesAsync();
                    return true;
                }
            }

            return false;
        }
    }
}


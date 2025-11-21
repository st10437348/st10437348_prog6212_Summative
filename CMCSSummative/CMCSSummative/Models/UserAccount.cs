namespace CMCSSummative.Models
{
    public class UserAccount
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public decimal HourlyRate { get; set; } = 0m;
    }
}


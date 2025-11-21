namespace CMCSPart2.Services
{
    public class SecurityOptions
    {
        public string EncryptionKeyBase64 { get; set; } = "";
        public string PrivateUploadsFolder { get; set; } = "App_Data/supporting-docs";
    }
}


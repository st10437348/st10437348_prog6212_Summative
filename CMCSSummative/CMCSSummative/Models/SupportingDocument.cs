namespace CMCSSummative.Models
{
    public class SupportingDocument
    {
        public int DocumentId { get; set; }
        public int ClaimId { get; set; }
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public DateTime UploadedAt { get; set; }
        public int UploadedByLecturerId { get; set; }
        public string FilePath { get; set; } = "";
        public string EncryptionIVBase64 { get; set; } = "";
        public long SizeBytes { get; set; }
    }
}




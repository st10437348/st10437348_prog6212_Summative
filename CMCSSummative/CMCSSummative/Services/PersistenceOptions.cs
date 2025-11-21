namespace CMCSPart2.Services
{
    public class PersistenceOptions
    {
        public bool Enabled { get; set; } = true;
        public string DataFile { get; set; } = "App_Data/cmcs-state.json";
        public bool EncryptState { get; set; } = false;
    }
}

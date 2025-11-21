using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CMCSSummative.Services
{
    public class FileEncryptionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly byte[] _key;
        private readonly string _privateRoot;

        public FileEncryptionService(IWebHostEnvironment env, IOptions<SecurityOptions> opt)
        {
            _env = env;
            var sec = opt.Value;
            if (string.IsNullOrWhiteSpace(sec.EncryptionKeyBase64))
                throw new InvalidOperationException("Security:EncryptionKeyBase64 missing");

            _key = Convert.FromBase64String(sec.EncryptionKeyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption key must be 32 bytes (AES-256)");

            _privateRoot = Path.Combine(env.ContentRootPath, sec.PrivateUploadsFolder);
            Directory.CreateDirectory(_privateRoot);
        }

        private static string Sanitize(string name) =>
            System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9\.\-_]+", "_");

        private (string dir, string encFile) BuildPrivatePath(int claimId, int docId, string safeName)
        {
            var dir = Path.Combine(_privateRoot, claimId.ToString());
            var enc = Path.Combine(dir, $"{docId}-{safeName}.enc");
            return (dir, enc);
        }

        public async Task<(string path, string ivBase64, long length)> SaveEncryptedAsync(int claimId, int docId, string fileName, Stream source)
        {
            var safeName = Sanitize(fileName);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            var ivB64 = Convert.ToBase64String(aes.IV);

            var (dir, encPath) = BuildPrivatePath(claimId, docId, safeName);
            Directory.CreateDirectory(dir);

            long len;
            using (var fs = File.Create(encPath))
            using (var crypto = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                await source.CopyToAsync(crypto);
                await crypto.FlushAsync();
                len = fs.Length;
            }

            return (encPath, ivB64, len);
        }

        public async Task<byte[]> DecryptAsync(string filePath, string ivBase64)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Encrypted file not found", filePath);

            var iv = Convert.FromBase64String(ivBase64);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var fs = File.OpenRead(filePath);
            using var crypto = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var ms = new MemoryStream();
            await crypto.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}

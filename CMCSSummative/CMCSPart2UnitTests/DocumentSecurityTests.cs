using System.Text;
using CMCSPart2.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMCSPart2.Tests
{
    public class DocumentSecurityTests
    {
        private static InMemoryStore MakeStore(out string root)
        {
            root = TestPaths.NewTempRoot();

            var key = new byte[32];
            Random.Shared.NextBytes(key);

            var env = new FakeEnv(root);
            var security = Options.Create(new SecurityOptions
            {
                EncryptionKeyBase64 = Convert.ToBase64String(key),
                PrivateUploadsFolder = "App_Data/supporting-docs"
            });
            var persistence = Options.Create(new PersistenceOptions
            {
                Enabled = true,
                DataFile = "App_Data/CMCSPart2-state.json",
                EncryptState = false
            });

            return new InMemoryStore(env, security, persistence);
        }

        [Fact]
        public async Task Upload_IsEncrypted_And_DecryptsBack()
        {
            var store = MakeStore(out _);

            var u = await store.GetOrCreateUserAsync("alex", "Lecturer");
            var l = await store.GetOrCreateLecturerForUserAsync(u.UserId, u.Username, "a@a.com");
            var claimId = await store.CreateClaimAsync(l.LecturerId, 1, 1, "doc");

            var original = Encoding.UTF8.GetBytes("Hello me");
            using var ms = new MemoryStream(original);

            var docId = await store.UploadDocumentAsync(
                claimId, l.LecturerId, "note.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ms);

            var doc = await store.GetDocumentAsync(docId);
            Assert.NotNull(doc);
            Assert.True(File.Exists(doc!.FilePath));
            Assert.NotEqual(original.Length, new FileInfo(doc.FilePath).Length);

            var roundtrip = await store.DecryptDocumentAsync(doc);
            Assert.Equal(original, roundtrip);
        }
    }
}


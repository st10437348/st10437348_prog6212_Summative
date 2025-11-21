using CMCSPart2.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMCSPart2.Tests
{
    public class PersistenceSnapshotTests
    {
        private static (InMemoryStore store, string root, string keyB64) MakeStoreWithPaths()
        {
            var root = TestPaths.NewTempRoot();

            var key = new byte[32];
            Random.Shared.NextBytes(key);
            var keyB64 = Convert.ToBase64String(key);

            var env = new FakeEnv(root);
            var security = Options.Create(new SecurityOptions
            {
                EncryptionKeyBase64 = keyB64,
                PrivateUploadsFolder = "App_Data/supporting-docs"
            });
            var persistence = Options.Create(new PersistenceOptions
            {
                Enabled = true,
                DataFile = "App_Data/CMCSPart2-state.json",
                EncryptState = false
            });

            var store = new InMemoryStore(env, security, persistence);
            return (store, root, keyB64);
        }

        [Fact]
        public async Task Snapshot_Persists_Claims_Across_Restart()
        {
            var (first, root, keyB64) = MakeStoreWithPaths();
            var user = await first.GetOrCreateUserAsync("bob", "Lecturer");
            var lec = await first.GetOrCreateLecturerForUserAsync(user.UserId, user.Username, "bob@lecturer.com");
            var claimId = await first.CreateClaimAsync(lec.LecturerId, 8m, 200m, "persist me");

            var env2 = new FakeEnv(root);
            var security2 = Options.Create(new SecurityOptions
            {
                EncryptionKeyBase64 = keyB64,
                PrivateUploadsFolder = "App_Data/supporting-docs"
            });
            var persistence2 = Options.Create(new PersistenceOptions
            {
                Enabled = true,
                DataFile = "App_Data/CMCSPart2-state.json",
                EncryptState = false
            });

            var second = new InMemoryStore(env2, security2, persistence2);
            await second.LoadAsync();

            var claim = await second.GetClaimAsync(claimId);
            Assert.NotNull(claim);
            Assert.Equal(lec.LecturerId, claim!.LecturerId);
            Assert.Equal(8m * 200m, claim.TotalAmount);
            Assert.Equal("Pending", claim.Status);
        }
    }
}

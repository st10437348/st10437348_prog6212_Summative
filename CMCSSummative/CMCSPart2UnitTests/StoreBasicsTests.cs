using CMCSPart2.Services;
using CMCSPart2.Models; 
using Microsoft.Extensions.Options;
using Xunit;

namespace CMCSPart2.Tests
{
    public class StoreBasicsTests
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
        public async Task UserLecturerCreation_AssignsIdsAndLinks()
        {
            var store = MakeStore(out _);

            var user = await store.GetOrCreateUserAsync("alex", "Lecturer");
            Assert.True(user.UserId > 0);
            Assert.Equal("Lecturer", user.Role);

            var lec = await store.GetOrCreateLecturerForUserAsync(user.UserId, user.Username, "alex@lecturer.com");
            Assert.True(lec.LecturerId > 0);
            Assert.Equal(user.UserId, lec.UserId);

            var lec2 = await store.GetOrCreateLecturerForUserAsync(user.UserId, user.Username, "alex@lecturer.com");
            Assert.Equal(lec.LecturerId, lec2.LecturerId);
        }
    }
}


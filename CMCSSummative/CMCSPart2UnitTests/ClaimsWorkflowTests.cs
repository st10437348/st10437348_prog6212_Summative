using CMCSSummative.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMCSPart2.Tests
{
    public class ClaimsWorkflowTests
    {
        private static InMemoryStore MakeStore()
        {
            var root = TestPaths.NewTempRoot();

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
        public async Task CreateClaim_ComputesTotal_AndStartsPending()
        {
            var store = MakeStore();

            var user = await store.GetOrCreateUserAsync("alex", "Lecturer");
            var lec = await store.GetOrCreateLecturerForUserAsync(user.UserId, user.Username, "alex@lecturer.com");

            var id = await store.CreateClaimAsync(lec.LecturerId, 10m, 123.45m, "hello");
            var claim = await store.GetClaimAsync(id);

            Assert.NotNull(claim);
            Assert.Equal(lec.LecturerId, claim!.LecturerId);
            Assert.Equal(10m * 123.45m, claim.TotalAmount);
            Assert.Equal("Pending", claim.Status);
        }

        [Fact]
        public async Task ApproveClaim_UpdatesStatus_AndLatestApproval()
        {
            var store = MakeStore();

            var u = await store.GetOrCreateUserAsync("alex", "Lecturer");
            var l = await store.GetOrCreateLecturerForUserAsync(u.UserId, u.Username, "x@y.com");

            var id = await store.CreateClaimAsync(l.LecturerId, 5m, 100m, "ok");
            await store.UpdateClaimStatusAsync(id, "Approved", "Coordinator", "looks good");

            var updated = await store.GetClaimAsync(id);
            Assert.Equal("Approved", updated!.Status);

            var latest = await store.GetLatestApprovalAsync(id);
            Assert.NotNull(latest);
            Assert.Equal("Approved", latest!.Decision);
            Assert.Equal("Coordinator", latest.ApprovedBy);
        }
    }
}


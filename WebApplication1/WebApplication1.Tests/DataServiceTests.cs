using System.IO;
using WebApplication1.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Xunit;


namespace WebApplication1.Tests
{
    public class DataServiceTests
    {
        private readonly DataService _dataService;

        public DataServiceTests()
        {
            var env = new FakeEnvironment(); // mock hosting environment with temp folders
            var logger = NullLogger<DataService>.Instance; // no-op logger
            _dataService = new DataService(env, logger);
        }

        [Fact]
        public void AddClaim_ShouldIncreaseClaimCount()
        {
            var before = _dataService.GetAllClaims().Count;
            _dataService.AddClaim(new Claim { LecturerName = "Test", HoursWorked = 5, HourlyRate = 100 });
            var after = _dataService.GetAllClaims().Count;

            Assert.True(after > before);
        }

        [Fact]
        public void UpdateClaimStatus_ShouldChangeStatusAndApprovedBy()
        {
            var claim = new Claim { LecturerName = "Test2", HoursWorked = 8, HourlyRate = 120 };
            _dataService.AddClaim(claim);

            _dataService.UpdateClaimStatus(claim.ClaimId, "Approved", "Manager");
            var updated = _dataService.GetClaimById(claim.ClaimId);

            Assert.Equal("Approved", updated.Status);
            Assert.Equal("Manager", updated.ApprovedBy);
        }

        [Fact]
        public void GetClaimsByStatus_ShouldReturnOnlyMatchingClaims()
        {
            _dataService.AddClaim(new Claim { LecturerName = "Alice", HoursWorked = 6, HourlyRate = 90, Status = "Pending" });
            _dataService.AddClaim(new Claim { LecturerName = "Bob", HoursWorked = 7, HourlyRate = 100, Status = "Verified" });

            var pending = _dataService.GetClaimsByStatus("Pending");

            Assert.All(pending, c => Assert.Equal("Pending", c.Status));
        }

        [Fact]
        public void AddDocumentToClaim_ShouldAttachDocumentSuccessfully()
        {
            var claim = new Claim { LecturerName = "DocTest", HoursWorked = 10, HourlyRate = 100 };
            _dataService.AddClaim(claim);

            var doc = new Document
            {
                FileName = "test.pdf",
                StoredFileName = "fakepath",
                FileType = ".pdf",
                FileSize = 1234
            };

            _dataService.AddDocumentToClaim(claim.ClaimId, doc);

            var updated = _dataService.GetClaimById(claim.ClaimId);
            Assert.Single(updated.Documents);
            Assert.Equal(".pdf", updated.Documents[0].FileType);
        }

        [Fact]
        public void TotalAmount_ShouldBeCalculatedCorrectly()
        {
            var claim = new Claim { HoursWorked = 10, HourlyRate = 150 };
            Assert.Equal(1500, claim.TotalAmount);
        }
    }

    // ✅ Mock environment compatible with .NET 9 and isolated for tests
    public class FakeEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";

        public IFileProvider WebRootFileProvider { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }

        public FakeEnvironment()
        {
            // Use temp folders so tests are isolated and do not depend on disk
            ContentRootPath = Path.Combine(Path.GetTempPath(), "TestContent_" + Guid.NewGuid());
            WebRootPath = Path.Combine(Path.GetTempPath(), "TestWebRoot_" + Guid.NewGuid());
            Directory.CreateDirectory(ContentRootPath);
            Directory.CreateDirectory(WebRootPath);

            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }
    }
    // This test suite verifies the core functionalities of the DataService class, which manages claim records 
    // and associated documents in a web application. It uses xUnit for testing and a mock hosting environment 
    // to isolate file system dependencies.
    //
    // Test Setup:
    // - A FakeEnvironment class simulates IWebHostEnvironment using temporary directories for WebRoot and ContentRoot.
    // - A NullLogger is used to suppress logging during tests.
    // - DataService is initialized with the fake environment and logger.
    //
    // Test Cases:
    // 1. AddClaim_ShouldIncreaseClaimCount:
    //    - Verifies that adding a new claim increases the total number of claims stored.
    //
    // 2. UpdateClaimStatus_ShouldChangeStatusAndApprovedBy:
    //    - Ensures that updating a claim's status and approver correctly modifies the claim's properties.
    //
    // 3. GetClaimsByStatus_ShouldReturnOnlyMatchingClaims:
    //    - Confirms that filtering claims by status returns only those with the specified status.
    //
    // 4. AddDocumentToClaim_ShouldAttachDocumentSuccessfully:
    //    - Tests that a document can be successfully attached to a claim and its metadata is stored correctly.
    //
    // 5. TotalAmount_ShouldBeCalculatedCorrectly:
    //    - Validates that the TotalAmount property of a claim is computed as HoursWorked × HourlyRate.
    //
    // Supporting Class:
    // - FakeEnvironment:
    //   - Implements IWebHostEnvironment with temporary folders to ensure tests do not rely on actual disk paths.
    //   - Provides physical file providers for both WebRoot and ContentRoot, enabling file operations in isolation.
}

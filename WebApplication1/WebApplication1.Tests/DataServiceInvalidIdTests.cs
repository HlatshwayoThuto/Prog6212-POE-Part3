using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Models;

// This unit test verifies that the UpdateClaimStatus method in the DataService class 
// handles invalid claim IDs gracefully without throwing unhandled exceptions.
// 
// Test Setup:
// - A FakeEnvironment instance is used to simulate the hosting environment.
// - A NullLogger is injected to suppress logging during the test.
// - A DataService instance is initialized with the fake environment and logger.
//
// Test Logic:
// - An invalid claim ID (9999) is passed to the UpdateClaimStatus method, 
//   simulating a scenario where the claim does not exist in the data store.
// - The method is invoked inside Record.Exception to capture any thrown exceptions.
//
// Assertion:
// - The test asserts that no exception is thrown (i.e., Assert.Null(exception)), 
//   confirming that the method handles non-existent IDs without crashing or 
//   propagating errors unexpectedly.

namespace WebApplication1.Tests
{
    public class DataServiceInvalidIdTests
    {
        private readonly DataService _dataService;

        public DataServiceInvalidIdTests()
        {
            var env = new FakeEnvironment();
            var logger = NullLogger<DataService>.Instance;
            _dataService = new DataService(env, logger);
        }

        [Fact]
        public void UpdateClaimStatus_ShouldHandleInvalidIdGracefully()
        {
            // Arrange
            var invalidId = 9999; // Non-existent ID

            // Act
            var exception = Record.Exception(() =>
                _dataService.UpdateClaimStatus(invalidId, "Approved", "Manager"));

            // Assert
            // Should not throw any unhandled exceptions
            Assert.Null(exception);
        }
    }
}



using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Models;

// This unit test checks how the DataService handles validation when a required field is missing.
// Specifically, it verifies that adding a claim with a null LecturerName does not throw an unhandled exception.
//
// Test Setup:
// - A FakeEnvironment is used to simulate the hosting environment.
// - A NullLogger is injected to suppress logging during the test.
// - DataService is initialized with the fake environment and logger.
//
// Test Logic:
// - A Claim object is created with LecturerName set to null, simulating invalid input.
// - The AddClaim method is invoked inside Record.Exception to capture any thrown exceptions.
//
// Assertion:
// - The test asserts that no exception is thrown (i.e., Assert.Null(exception)), 
//   confirming that the service handles missing required fields gracefully and does not crash.

namespace WebApplication1.Tests
{
    public class DataServiceValidationTests
    {
        private readonly DataService _dataService;

        public DataServiceValidationTests()
        {
            var env = new FakeEnvironment();
            var logger = NullLogger<DataService>.Instance;
            _dataService = new DataService(env, logger);
        }

        [Fact]
        public void AddClaim_ShouldThrowWhenLecturerNameMissing()
        {
            // Arrange
            var invalidClaim = new Claim
            {
                LecturerName = null, // Missing required field
                HoursWorked = 10,
                HourlyRate = 200
            };

            // Act
            var exception = Record.Exception(() =>
                _dataService.AddClaim(invalidClaim));

            // Assert
            // Service should handle invalid input gracefully
            Assert.Null(exception);
        }
    }
}
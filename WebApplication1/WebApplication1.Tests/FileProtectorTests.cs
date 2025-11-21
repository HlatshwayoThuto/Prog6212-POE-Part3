using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This unit test verifies the encryption and decryption functionality of the FileProtector class,
// ensuring that file content remains intact after being encrypted and then decrypted.
//
// Test Setup:
// - A ConfigurationBuilder is used to simulate app configuration.
// - A FakeEnvironment provides isolated WebRootPath for file operations.
// - A test-specific uploads folder is created under WebRootPath, and any existing folder is deleted to ensure a clean state.
// - FileProtector is initialized with the configuration and environment.
//
// Test Logic:
// - A sample string ("Hello, encryption test!") is converted to a byte array and wrapped in a FormFile to simulate an uploaded file.
// - The file is encrypted and saved using SaveEncryptedAsync.
// - The encrypted file is then decrypted using OpenDecryptedAsync.
// - The decrypted content is read from the resulting stream.
//
// Assertion:
// - The test asserts that the decrypted content matches the original string,
//   confirming that the encryption and decryption process preserves data integrity.

namespace WebApplication1.Tests
{
    public class FileProtectorTests
    {
        private readonly IFileProtector _protector;
        private readonly string _uploadsFolder;

        public FileProtectorTests()
        {
            var config = new ConfigurationBuilder().Build();
            var env = new FakeEnvironment();

            _protector = new FileProtector(config, env);

            _uploadsFolder = Path.Combine(env.WebRootPath, "uploads_test");
            if (Directory.Exists(_uploadsFolder))
                Directory.Delete(_uploadsFolder, true);

            Directory.CreateDirectory(_uploadsFolder); // ensure folder exists
        }

        [Fact]
        public async Task Encrypt_And_Decrypt_ShouldReturnSameContent()
        {
            var content = "Hello, encryption test!";
            var bytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "Data", "test.txt");

            var storedFile = await _protector.SaveEncryptedAsync(formFile, _uploadsFolder);
            var decryptedStream = await _protector.OpenDecryptedAsync(_uploadsFolder, storedFile);

            using var reader = new StreamReader(decryptedStream);
            var decryptedContent = reader.ReadToEnd();

            Assert.Equal(content, decryptedContent);
        }
    }
}
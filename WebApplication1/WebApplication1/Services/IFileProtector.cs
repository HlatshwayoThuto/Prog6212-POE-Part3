using System.Security.Cryptography;       // Provides cryptographic services including AES encryption
using Microsoft.AspNetCore.Hosting;       // Used to access web hosting environment properties

// Interface defining methods for secure file encryption and decryption
public interface IFileProtector
{
    /// <summary>
    /// Encrypts and saves the uploaded file to disk. Returns the stored filename.
    /// </summary>
    Task<string> SaveEncryptedAsync(IFormFile file, string uploadsFolder);

    /// <summary>
    /// Opens and returns a decrypted stream of the stored encrypted file.
    /// </summary>
    Task<Stream> OpenDecryptedAsync(string uploadsFolder, string storedFileName);

    /// <summary>
    /// Returns the encryption key and IV as a base64-encoded string.
    /// </summary>
    string GetKeyIdentifier();
}

// Concrete implementation of IFileProtector using AES encryption
public class FileProtector : IFileProtector
{
    // AES encryption key and initialization vector
    private readonly byte[] _key;
    private readonly byte[] _iv;

    // Constructor initializes encryption key and IV, loading from disk or generating new ones
    public FileProtector(IConfiguration config, IWebHostEnvironment env)
    {
        // Define the path to the key file inside App_Data
        var keyFile = Path.Combine(env.ContentRootPath, "App_Data", "file_key.txt");

        // Ensure the directory exists
        if (!Directory.Exists(Path.GetDirectoryName(keyFile)))
            Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);

        // If key file exists, read and parse the stored key and IV
        if (File.Exists(keyFile))
        {
            var combined = File.ReadAllText(keyFile);
            var parts = combined.Split(':');
            _key = Convert.FromBase64String(parts[0]);
            _iv = Convert.FromBase64String(parts[1]);
        }
        else
        {
            // Generate new AES key and IV
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();
            _key = aes.Key;
            _iv = aes.IV;

            // Save the key and IV to disk in base64 format
            var combined = Convert.ToBase64String(_key) + ":" + Convert.ToBase64String(_iv);
            File.WriteAllText(keyFile, combined);
        }
    }

    // Returns the key and IV as a base64-encoded string for identification or debugging
    public string GetKeyIdentifier() => Convert.ToBase64String(_key) + ":" + Convert.ToBase64String(_iv);

    // Encrypts the uploaded file and saves it to the specified folder
    public async Task<string> SaveEncryptedAsync(IFormFile file, string uploadsFolder)
    {
        // Ensure the uploads folder exists
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        // Generate a unique filename for the encrypted file
        var storedFileName = $"{Guid.NewGuid():N}.bin";
        var storedPath = Path.Combine(uploadsFolder, storedFileName);

        // Create a file stream for writing the encrypted content
        using var outFs = new FileStream(storedPath, FileMode.CreateNew);

        // Create AES encryptor using the stored key and IV
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        // Wrap the file stream in a CryptoStream for encryption
        using var crypto = new CryptoStream(outFs, aes.CreateEncryptor(), CryptoStreamMode.Write);

        // Copy the uploaded file into the encrypted stream
        await file.CopyToAsync(crypto);
        await crypto.FlushAsync();

        // Return the stored filename for reference
        return storedFileName;
    }

    // Opens and decrypts the stored encrypted file, returning a readable stream
    public Task<Stream> OpenDecryptedAsync(string uploadsFolder, string storedFileName)
    {
        // Build the full path to the encrypted file
        var storedPath = Path.Combine(uploadsFolder, storedFileName);

        // Throw an error if the file doesn't exist
        if (!File.Exists(storedPath)) throw new FileNotFoundException("Encrypted file missing.", storedPath);

        // Create a memory stream to hold the decrypted content
        var mem = new MemoryStream();

        // Open the encrypted file and decrypt its contents into memory
        using (var inFs = File.OpenRead(storedPath))
        using (var aes = Aes.Create())
        using (var crypto = new CryptoStream(inFs, aes.CreateDecryptor(_key, _iv), CryptoStreamMode.Read))
        {
            crypto.CopyTo(mem);
        }

        // Reset stream position to the beginning
        mem.Position = 0;

        // Return the decrypted stream
        return Task.FromResult<Stream>(mem);
    }
}
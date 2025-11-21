// Required namespaces for JSON serialization, hosting environment access, and logging
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Models
{
    public class DataService
    {
        // In-memory lists
        private readonly List<Claim> _claims = new();
        private readonly List<User> _users = new();

        // Auto-incrementing IDs
        private int _nextClaimId = 1;
        private int _nextDocumentId = 1;
        private int _nextUserId = 1;

        // File paths
        private readonly string _dataFilePath;
        private readonly string _usersFilePath;
        private readonly string _uploadsFolder;

        private readonly ILogger<DataService> _logger;
        private readonly object _lock = new();

        public DataService(IWebHostEnvironment env, ILogger<DataService> logger)
        {
            _logger = logger;

            // Create App_Data folder if it doesn't exist
            var dataFolder = Path.Combine(env.ContentRootPath, "App_Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

            _dataFilePath = Path.Combine(dataFolder, "claims_data.json");
            _usersFilePath = Path.Combine(dataFolder, "users_data.json");

            _uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            if (!Directory.Exists(_uploadsFolder)) Directory.CreateDirectory(_uploadsFolder);

            // Load persisted data if present
            LoadData();
            LoadUsers();
        }

        // Expose uploads folder path
        public string GetUploadsFolder() => _uploadsFolder;

        // ----------------------
        // Claims
        // ----------------------

        private void SaveData()
        {
            lock (_lock)
            {
                try
                {
                    var container = new
                    {
                        Claims = _claims,
                        NextClaimId = _nextClaimId,
                        NextDocumentId = _nextDocumentId
                    };

                    var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_dataFilePath, json);
                    _logger.LogInformation("Saved claims ({Count})", _claims.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving claims data");
                }
            }
        }

        private void LoadData()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_dataFilePath)) return;

                    var json = File.ReadAllText(_dataFilePath);
                    var doc = JsonSerializer.Deserialize<JsonElement>(json);

                    var claims = JsonSerializer.Deserialize<List<Claim>>(doc.GetProperty("Claims").GetRawText());
                    var nextClaimId = doc.GetProperty("NextClaimId").GetInt32();
                    var nextDocId = doc.GetProperty("NextDocumentId").GetInt32();

                    if (claims != null)
                    {
                        _claims.Clear();
                        _claims.AddRange(claims);
                        _nextClaimId = Math.Max(1, nextClaimId);
                        _nextDocumentId = Math.Max(1, nextDocId);
                    }

                    _logger.LogInformation("Loaded {Count} claims", _claims.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load claims data");
                    _claims.Clear();
                    _nextClaimId = 1;
                    _nextDocumentId = 1;
                }
            }
        }

        public List<Claim> GetAllClaims()
        {
            lock (_lock) return _claims.Select(c => Clone(c)).ToList();
        }

        public Claim? GetClaimById(int id)
        {
            lock (_lock) return _claims.FirstOrDefault(c => c.ClaimId == id);
        }

        public void AddClaim(Claim claim)
        {
            lock (_lock)
            {
                claim.ClaimId = _nextClaimId++;
                claim.SubmissionDate = DateTime.Now;
                _claims.Add(claim);
                SaveData();
            }
        }

        public void UpdateClaimStatus(int claimId, string status, string approvedBy = "")
        {
            lock (_lock)
            {
                var claim = _claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim == null) return;

                claim.Status = status;
                claim.ApprovalDate = DateTime.Now;
                claim.ApprovedBy = approvedBy;

                SaveData();
            }
        }

        public void AddDocumentToClaim(int claimId, Document doc)
        {
            lock (_lock)
            {
                var claim = _claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim == null) return;

                doc.DocumentId = _nextDocumentId++;
                doc.ClaimId = claimId;
                doc.UploadDate = DateTime.Now;

                claim.Documents.Add(doc);
                SaveData();
            }
        }

        public List<Claim> GetClaimsByStatus(string status)
        {
            lock (_lock)
            {
                return _claims
                    .Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase))
                    .Select(c => Clone(c))
                    .ToList();
            }
        }

        private Claim Clone(Claim c)
        {
            return new Claim
            {
                ClaimId = c.ClaimId,
                LecturerId = c.LecturerId,
                LecturerName = c.LecturerName,
                HoursWorked = c.HoursWorked,
                HourlyRate = c.HourlyRate,
                Notes = c.Notes,
                Status = c.Status,
                SubmissionDate = c.SubmissionDate,
                ApprovalDate = c.ApprovalDate,
                ApprovedBy = c.ApprovedBy,
                Documents = c.Documents.Select(d => new Document
                {
                    DocumentId = d.DocumentId,
                    ClaimId = d.ClaimId,
                    FileName = d.FileName,
                    StoredFileName = d.StoredFileName,
                    UploadDate = d.UploadDate,
                    FileSize = d.FileSize,
                    FileType = d.FileType
                }).ToList()
            };
        }

        // ----------------------
        // Users
        // ----------------------

        private void SaveUsers()
        {
            lock (_lock)
            {
                try
                {
                    var container = new
                    {
                        Users = _users,
                        NextUserId = _nextUserId
                    };

                    var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_usersFilePath, json);
                    _logger.LogInformation("Saved users ({Count})", _users.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving users data");
                }
            }
        }

        private void LoadUsers()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_usersFilePath)) return;

                    var json = File.ReadAllText(_usersFilePath);
                    var doc = JsonSerializer.Deserialize<JsonElement>(json);

                    var users = JsonSerializer.Deserialize<List<User>>(doc.GetProperty("Users").GetRawText());
                    var nextUserId = doc.GetProperty("NextUserId").GetInt32();

                    if (users != null)
                    {
                        _users.Clear();
                        _users.AddRange(users);
                        _nextUserId = Math.Max(1, nextUserId);
                    }

                    _logger.LogInformation("Loaded {Count} users", _users.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load users data");
                    _users.Clear();
                    _nextUserId = 1;
                }
            }
        }

        public User? GetUserByEmail(string email)
        {
            lock (_lock) return _users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        public User? ValidateUser(string email, string passwordHash)
        {
            lock (_lock) return _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase) &&
                u.PasswordHash == passwordHash);
        }

        public User AddUser(User user)
        {
            lock (_lock)
            {
                user.UserId = _nextUserId++;
                _users.Add(user);
                SaveUsers();
                return user;
            }
        }

        public User? GetUserById(int id)
        {
            lock (_lock) return _users.FirstOrDefault(u => u.UserId == id);
        }
    }
}
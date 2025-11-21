📘 Contract Monthly Claim System (CMCS)

A secure web-based system for managing lecturer claims, coordinator reviews, manager approvals, and HR administrative functions.
Developed for PROG6212 POE – Part 3.

📌 Table of Contents

Overview

System Roles

Key Features (Part 3)

Technologies Used

Database Schema

Installation & Setup

System Workflow

Security Features

Project Structure

Screenshots

Credits

📌 Overview

The Contract Monthly Claim System (CMCS) streamlines the workflow for independent contractor lecturers.
It allows secure submission, processing, approval, and reporting of monthly contract claims.

This version includes all Part 3 upgrades, such as encrypted file storage, CSV exporting, corrected decimal precision, and improved user interface.

👥 System Roles
1. Lecturer

Submits new claims

Uploads encrypted supporting documents

Tracks submitted claim statuses

2. Coordinator

Reviews lecturer claims

Requests corrections

Downloads attached documents

3. Manager

Provides final approval

Can view/download documents

Sends approved claims to HR

4. HR Administrator

Manages user accounts

Assigns Hourly Rates

Generates system-wide CSV reports

✨ Key Features (Part 3)
✔ Hourly Rate Assigned by HR

Lecturers no longer enter their own rate — it is loaded automatically from their profile.

✔ Secure Document Encryption

All uploaded files are stored encrypted using the IFileProtector service.

✔ Coordinator/Manager Download Buttons

Long file names are replaced with:

[ Download Document ]


Prevents layout breakage.

✔ CSV Reporting for HR

HR can export all claims using:

/HR/ExportClaimsCsv

✔ Role-Based Access Control

Each controller is protected with:

[Authorize(Roles = "RoleName")]

✔ Accurate Decimal Handling

Claims use:

decimal(10,2)


preventing miscalculations.

🛠 Technologies Used
Technology	Description
ASP.NET Core MVC	Main web framework
Entity Framework Core	Database ORM
SQL Server	Persistent database
Bootstrap 5	Front-end UI styling
PdfSharpCore	(Earlier PDF support, replaced by CSV output)
CSV Export	Built-in text generation for reports
Dependency Injection	Used for DB + file encryption
🗄 Database Schema
Tables

Users

Claims

Documents

Key Columns

decimal(10,2) for money

Notes column added

Foreign key: Claim → Documents

⚙ Installation & Setup
1. Clone the repository
git clone https://github.com/your-repo-name

2. Update appsettings.json

Set your database connection string:

"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CMCS;Trusted_Connection=True;"
}

3. Apply EF Migrations
Update-Database

4. Run the project
dotnet run

5. Create HR user manually

Because HR must log in first, manually insert:

INSERT INTO Users (Name, Surname, Email, Role, PasswordHash, HourlyRate)
VALUES ('Admin', 'HR', 'hr@example.com', 'HR', 'HASHEDPASSWORD', 0);

🔄 System Workflow
Lecturer

➡ Submit claim → Upload docs → Track progress

Coordinator

➡ Review → Approve/Reject → Forward to Manager

Manager

➡ Final Approval

HR

➡ Generate CSV → Manage Users

🔐 Security Features

SHA256 password hashing

Document encryption at rest

Role-based access

Server-side file validation

File type + size restrictions (PDF/DOCX/XLSX, max 5MB)

📁 Project Structure
Controllers/
    LecturerController.cs
    CoordinatorController.cs
    ManagerController.cs
    HRController.cs
Models/
    User.cs
    Claim.cs
    Document.cs
Services/
    IFileProtector.cs
Views/
    Lecturer/
    Coordinator/
    Manager/
    HR/
wwwroot/
    css/
    js/

🖼 Screenshots

(You will insert screenshots here in your Word/PDF submission.)
✔ Submit Claim
✔ Track Claims
✔ Coordinator Review
✔ Manager Approval
✔ HR CSV Export

👨‍🎓 Credits

Developed by: Thuto
Module: PROG6212
Institution: IIE Msa
Year: 2025
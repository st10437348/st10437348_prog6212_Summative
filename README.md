# Contract Monthly Claim System (CMCS) – Summative (Updated After Part 2)



This project is a web-based system built using ASP.NET Core MVC. It enables lecturers to submit monthly claims, upload supporting documents and track their approval status. Coordinators and managers can review, approve or reject submitted claims. An HR role has been added to manage users and lecturer data. The system now uses a SQL Server database instead of an in-memory JSON store.



## YouTube Link



https://youtu.be/GPmWTDFGJs8



## Setup Instructions



Clone the repository.



Open the solution in Visual Studio 2022.



Ensure that the .NET 9.0 SDK is installed.



Ensure that appsettings.json contains a valid SQL Server connection string under "ConnectionStrings:DefaultConnection".



Ensure that appsettings.json contains a valid 32-byte AES key under "Security:EncryptionKeyBase64".



Build the project to restore dependencies.



Run the application.



## Login details of key existing users



Lect1 – password: lecturer (Lecturer)

Coord – password: coordinator (Coordinator

mgr – password: manager (Manager)

hradmin – password: hrpass (HR)



(All accounts and roles are validated against the Users table in the database.)



## Changes Since Part 2



#### Added



• Full SQL Server database using Entity Framework Core (replacing InMemoryStore).

• HR role with dashboard, user management and editing capabilities.

• Automated claim validation service (hours > 150, required documents, etc.).

• Auto-approve and auto-reject logic based on predefined criteria.

• Role-validated login (selected role must match stored database role).

• User deletion for HR.

• Improved action layout on Coordinator and Manager claims pages.

• Updated document download buttons to show “Download” instead of filenames.

• Improved server-side and client-side validation.

• Lecturer hourly rate now stored in the database.

• Automatic creation of Lecturer profiles when HR creates users.

• Session-based access control for all roles.



#### Removed



• InMemoryStore class.

• JSON-based persistence (App\_Data/CMCSPart2-state.json).

• All snapshot persistence options.

• Role-selection-only login (replaced with username/password security).

• All unit tests dependent on InMemoryStore and persistence snapshots.

• Any JSON encryption related to state storage.



## Pages and Views



• Home/Index – Login page with database role validation.

• Lecturers/Index – Lecturer dashboard showing lecturer information.

• Lecturers/Create – Submit a new claim (total calculated automatically). Includes validation for maximum hours.

• Lecturers/Details – View submitted claims, approval status, and documents.

• Lecturers/UploadDocument – Upload PDF, DOCX, or XLSX files (AES-encrypted).



• Coordinator/Index – Coordinator dashboard.

• Coordinator/Claims – View all claims; automatic rules applied to pending claims.

• Coordinator/Edit – Review an individual claim, leave comments and approve or reject.



• Manager/Index – Manager dashboard.

• Manager/Claims – View all claims; automatic rules applied to pending claims.

• Manager/Edit – Review an individual claim, leave comments and approve or reject.



• HR/Index – HR dashboard.

• HR/Users – View all system users.

• HR/CreateUser – Add new accounts (automatically creates Lecturer profiles when needed).

• HR/EditUser – Update account information, including hourly rate.

• HR/DeleteUser – Remove users from the system.

• HR/GenerateReport – Generate downloadable PDF summaries.



## Technologies Used



• ASP.NET Core MVC

• C#

• SQL Server Management Studion 2022 / Azure SQL

• Entity Framework Core

• Bootstrap

• AES-256 encryption for file storage



## Data and Security



• All data is now stored in a SQL Server database.

• Uploaded documents are encrypted using AES-256 CBC mode and stored on disk under App\_Data/supporting-docs.

• Documents are decrypted automatically when downloaded.

• Session-based security is used to restrict access by role.

• Claims store hours worked, hourly rate, total amount, approval history and related files.

• Automatic claim validation ensures consistency, limits, and compliance with policy.



## Unit Tests



All tests from Part 2 relying on the InMemoryStore snapshot system have been removed, as the project now uses SQL Server.



Configuration File (appsettings.json)



## Security

• EncryptionKeyBase64 – 32-byte key for AES encryption.

• PrivateUploadsFolder – Directory for encrypted file storage.



## Database

• ConnectionStrings:DefaultConnection – SQL Server connection.



(The previous "Persistence" section has been removed.)



## Notes



• The system no longer uses in-memory data or JSON persistence.

• All functionality is now fully database driven.

• HR now manages users, rates, and lecturer profiles.

• The approval workflow shows role-based decisions (Coordinator/Manager).

• Automatic claim validation and auto-decisioning are now included.



## References



Ctrl+Alt+Teach (n.d.) Unit Test Tutorials MVC. YouTube video. Available at: https://www.youtube.com/watch?v=WCX1IXUo0ho



Digital TechJoint (n.d.) Implementing AES 256 Encryption in ASP.NET – Step-by-Step Tutorial. YouTube video. Available at: https://www.youtube.com/watch?v=LctYdd-fen8



Microsoft Docs. (n.d.) Unit testing C# in .NET using dotnet test and xUnit. Available at: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit



Stack Overflow. (2009) How do I connect to a SQL database from C#? Available at: https://stackoverflow.com/questions/1345508/how-do-i-connect-to-a-sql-database-from-c (Accessed: 19 November 2025)



Stack Overflow. (2016) Adding a specific error message to a View in MVC. Available at: https://stackoverflow.com/questions/37115792/adding-a-specific-error-message-to-a-view-in-mvc (Accessed: 20 November 2025).



Troelsen, A. and Japikse, P., 2022. Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. 11th ed. Berkeley, CA: Apress.


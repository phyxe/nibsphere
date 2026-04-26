using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Database
{
	public sealed class AcademicsDatabaseInitializer : IModuleDatabaseInitializer
	{
		public string ModuleKey => "academics";

		public int SortOrder => 300;

		public async Task InitializeAsync(
			IAppPaths appPaths,
			CancellationToken cancellationToken = default)
		{
			LocalDbConnectionFactory connectionFactory = new(appPaths);

			using SqlConnection connection = connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			string sql =
				"""
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_Teacher')
                BEGIN
                    CREATE TABLE Academics_Teacher
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        LastName NVARCHAR(100) NOT NULL,
                        FirstName NVARCHAR(100) NOT NULL,
                        MiddleName NVARCHAR(100) NULL,
                        ExtensionName NVARCHAR(50) NULL,

                        Position NVARCHAR(150) NULL,
                        Designation NVARCHAR(150) NULL,

                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_EnrollmentStatus')
                BEGIN
                    CREATE TABLE Academics_EnrollmentStatus
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        Code NVARCHAR(50) NOT NULL,
                        Name NVARCHAR(100) NOT NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_Program')
                BEGIN
                    CREATE TABLE Academics_Program
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        Code NVARCHAR(50) NOT NULL,
                        Name NVARCHAR(150) NOT NULL,
                        Description NVARCHAR(500) NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_ProgramProspectusLine')
                BEGIN
                    CREATE TABLE Academics_ProgramProspectusLine
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        ProgramId INT NOT NULL,

                        GradeLevelName NVARCHAR(50) NOT NULL,

                        TemplateTermSequence INT NOT NULL DEFAULT 1,
                        TemplateTermLabel NVARCHAR(100) NOT NULL,

                        LearningAreaId INT NOT NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SectionTemplate')
                BEGIN
                    CREATE TABLE Academics_SectionTemplate
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        GradeLevelName NVARCHAR(50) NOT NULL,
                        SectionName NVARCHAR(100) NOT NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYear')
                BEGIN
                    CREATE TABLE Academics_SchoolYear
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        Name NVARCHAR(100) NOT NULL,

                        StartDate DATE NULL,
                        EndDate DATE NULL,

                        IsCurrent BIT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYearTerm')
                BEGIN
                    CREATE TABLE Academics_SchoolYearTerm
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearId INT NOT NULL,

                        ParentTermId INT NULL,

                        Name NVARCHAR(100) NOT NULL,
                        ShortName NVARCHAR(50) NULL,

                        StartDate DATE NULL,
                        EndDate DATE NULL,

                        SortOrder INT NOT NULL DEFAULT 0,

                        IsEnrollmentTerm BIT NOT NULL DEFAULT 0,
                        IsGradingTerm BIT NOT NULL DEFAULT 1,
                        IsReportingTerm BIT NOT NULL DEFAULT 1,

                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_ProgramProspectusLine_Program'
                )
                BEGIN
                    ALTER TABLE Academics_ProgramProspectusLine
                    ADD CONSTRAINT FK_Academics_ProgramProspectusLine_Program
                        FOREIGN KEY (ProgramId)
                        REFERENCES Academics_Program(Id);
                END

                IF OBJECT_ID('LearningArea', 'U') IS NOT NULL
                   AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_ProgramProspectusLine_LearningArea'
                )
                BEGIN
                    ALTER TABLE Academics_ProgramProspectusLine
                    ADD CONSTRAINT FK_Academics_ProgramProspectusLine_LearningArea
                        FOREIGN KEY (LearningAreaId)
                        REFERENCES LearningArea(Id);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearTerm_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearTerm
                    ADD CONSTRAINT FK_Academics_SchoolYearTerm_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearTerm_ParentTerm'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearTerm
                    ADD CONSTRAINT FK_Academics_SchoolYearTerm_ParentTerm
                        FOREIGN KEY (ParentTermId)
                        REFERENCES Academics_SchoolYearTerm(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Teacher_Name'
                      AND object_id = OBJECT_ID('Academics_Teacher')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Teacher_Name
                    ON Academics_Teacher(LastName, FirstName, MiddleName);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_EnrollmentStatus_Code'
                      AND object_id = OBJECT_ID('Academics_EnrollmentStatus')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_EnrollmentStatus_Code
                    ON Academics_EnrollmentStatus(Code);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_EnrollmentStatus_Name'
                      AND object_id = OBJECT_ID('Academics_EnrollmentStatus')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_EnrollmentStatus_Name
                    ON Academics_EnrollmentStatus(Name);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_Program_Code'
                      AND object_id = OBJECT_ID('Academics_Program')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_Program_Code
                    ON Academics_Program(Code);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Program_Name'
                      AND object_id = OBJECT_ID('Academics_Program')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Program_Name
                    ON Academics_Program(Name);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_ProgramProspectusLine_Program'
                      AND object_id = OBJECT_ID('Academics_ProgramProspectusLine')
                )
                BEGIN
                    CREATE INDEX IX_Academics_ProgramProspectusLine_Program
                    ON Academics_ProgramProspectusLine
                    (
                        ProgramId,
                        GradeLevelName,
                        TemplateTermSequence,
                        SortOrder
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_ProgramProspectusLine_LearningArea'
                      AND object_id = OBJECT_ID('Academics_ProgramProspectusLine')
                )
                BEGIN
                    CREATE INDEX IX_Academics_ProgramProspectusLine_LearningArea
                    ON Academics_ProgramProspectusLine(LearningAreaId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SectionTemplate_GradeLevel_SectionName'
                      AND object_id = OBJECT_ID('Academics_SectionTemplate')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SectionTemplate_GradeLevel_SectionName
                    ON Academics_SectionTemplate(GradeLevelName, SectionName);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SchoolYear_Name'
                      AND object_id = OBJECT_ID('Academics_SchoolYear')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYear_Name
                    ON Academics_SchoolYear(Name);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SchoolYear_Current'
                      AND object_id = OBJECT_ID('Academics_SchoolYear')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYear_Current
                    ON Academics_SchoolYear(IsCurrent)
                    WHERE IsCurrent = 1;
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearTerm_SchoolYear'
                      AND object_id = OBJECT_ID('Academics_SchoolYearTerm')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearTerm_SchoolYear
                    ON Academics_SchoolYearTerm
                    (
                        SchoolYearId,
                        ParentTermId,
                        SortOrder,
                        Name
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearTerm_ParentTerm'
                      AND object_id = OBJECT_ID('Academics_SchoolYearTerm')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearTerm_ParentTerm
                    ON Academics_SchoolYearTerm(ParentTermId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'ENROLLED'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('ENROLLED', 'Enrolled', 10, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'TRANSFERRED_IN'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('TRANSFERRED_IN', 'Transferred In', 20, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'TRANSFERRED_OUT'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('TRANSFERRED_OUT', 'Transferred Out', 30, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'DROPPED'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('DROPPED', 'Dropped', 40, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'COMPLETED'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('COMPLETED', 'Completed', 50, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'PENDING'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('PENDING', 'Pending', 60, 1);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM Academics_EnrollmentStatus
                    WHERE Code = 'CANCELLED'
                )
                BEGIN
                    INSERT INTO Academics_EnrollmentStatus (Code, Name, SortOrder, IsActive)
                    VALUES ('CANCELLED', 'Cancelled', 70, 1);
                END
                """;

			using SqlCommand command = new(sql, connection);
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}
}
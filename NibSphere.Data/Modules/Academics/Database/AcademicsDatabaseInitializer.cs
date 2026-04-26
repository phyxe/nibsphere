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

                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYearProgram')
                BEGIN
                    CREATE TABLE Academics_SchoolYearProgram
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearId INT NOT NULL,
                        SourceProgramId INT NOT NULL,

                        ProgramCode NVARCHAR(50) NOT NULL,
                        ProgramName NVARCHAR(150) NOT NULL,
                        ProgramDescription NVARCHAR(500) NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYearProgramLine')
                BEGIN
                    CREATE TABLE Academics_SchoolYearProgramLine
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearProgramId INT NOT NULL,
                        SourceProgramProspectusLineId INT NOT NULL,

                        GradeLevelName NVARCHAR(50) NOT NULL,

                        TemplateTermSequence INT NOT NULL DEFAULT 1,
                        TemplateTermLabel NVARCHAR(100) NOT NULL,

                        LearningAreaId INT NOT NULL,

                        LearningAreaCode NVARCHAR(50) NOT NULL,
                        LearningAreaShortName NVARCHAR(150) NULL,
                        LearningAreaDescription NVARCHAR(300) NOT NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYearSection')
                BEGIN
                    CREATE TABLE Academics_SchoolYearSection
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearId INT NOT NULL,
                        SourceSectionTemplateId INT NULL,

                        GradeLevelName NVARCHAR(50) NOT NULL,
                        SectionName NVARCHAR(100) NOT NULL,

                        AdviserTeacherId INT NULL,

                        AdviserLastName NVARCHAR(100) NULL,
                        AdviserFirstName NVARCHAR(100) NULL,
                        AdviserMiddleName NVARCHAR(100) NULL,
                        AdviserExtensionName NVARCHAR(50) NULL,

                        AdviserPosition NVARCHAR(150) NULL,
                        AdviserDesignation NVARCHAR(150) NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SchoolYearSectionProgram')
                BEGIN
                    CREATE TABLE Academics_SchoolYearSectionProgram
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearSectionId INT NOT NULL,
                        SchoolYearProgramId INT NOT NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_Subject')
                BEGIN
                    CREATE TABLE Academics_Subject
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SchoolYearId INT NOT NULL,
                        TermId INT NOT NULL,
                        SectionId INT NOT NULL,

                        LearningAreaId INT NOT NULL,

                        SchoolYearProgramLineId INT NULL,

                        TeacherId INT NULL,

                        TeacherLastName NVARCHAR(100) NULL,
                        TeacherFirstName NVARCHAR(100) NULL,
                        TeacherMiddleName NVARCHAR(100) NULL,
                        TeacherExtensionName NVARCHAR(50) NULL,

                        TeacherPosition NVARCHAR(150) NULL,
                        TeacherDesignation NVARCHAR(150) NULL,

                        SubjectCode NVARCHAR(50) NULL,
                        SubjectName NVARCHAR(150) NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_SubjectScheduleSlot')
                BEGIN
                    CREATE TABLE Academics_SubjectScheduleSlot
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        SubjectId INT NOT NULL,

                        DayOfWeekNumber INT NOT NULL DEFAULT 0,
                        DayOfWeekName NVARCHAR(50) NOT NULL,

                        StartTime TIME NULL,
                        EndTime TIME NULL,

                        Room NVARCHAR(100) NULL,

                        SortOrder INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_Enrollment')
                BEGIN
                    CREATE TABLE Academics_Enrollment
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        LearnerId INT NOT NULL,

                        SchoolYearId INT NOT NULL,
                        SchoolYearProgramId INT NOT NULL,
                        SchoolYearSectionId INT NOT NULL,

                        GradeLevelName NVARCHAR(50) NOT NULL,

                        EnrollmentStatusId INT NOT NULL,

                        EnrollmentStatusCode NVARCHAR(50) NOT NULL,
                        EnrollmentStatusName NVARCHAR(100) NOT NULL,

                        EnrollmentDate DATE NULL,
                        EffectiveStartDate DATE NULL,
                        EffectiveEndDate DATE NULL,

                        IsCurrent BIT NOT NULL DEFAULT 1,
                        IsActive BIT NOT NULL DEFAULT 1,

                        Remarks NVARCHAR(500) NULL,

                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Academics_EnrollmentSubject')
                BEGIN
                    CREATE TABLE Academics_EnrollmentSubject
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),

                        EnrollmentId INT NOT NULL,
                        SubjectId INT NOT NULL,

                        EnrollmentStatusId INT NULL,

                        EnrollmentStatusCode NVARCHAR(50) NULL,
                        EnrollmentStatusName NVARCHAR(100) NULL,

                        EffectiveStartDate DATE NULL,
                        EffectiveEndDate DATE NULL,

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
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgram_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearProgram_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgram_SourceProgram'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearProgram_SourceProgram
                        FOREIGN KEY (SourceProgramId)
                        REFERENCES Academics_Program(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_SchoolYearProgram'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_SchoolYearProgram
                        FOREIGN KEY (SchoolYearProgramId)
                        REFERENCES Academics_SchoolYearProgram(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_SourceProspectusLine'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_SourceProspectusLine
                        FOREIGN KEY (SourceProgramProspectusLineId)
                        REFERENCES Academics_ProgramProspectusLine(Id);
                END

                IF OBJECT_ID('LearningArea', 'U') IS NOT NULL
                   AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_LearningArea'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_LearningArea
                        FOREIGN KEY (LearningAreaId)
                        REFERENCES LearningArea(Id);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearSection_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearSection
                    ADD CONSTRAINT FK_Academics_SchoolYearSection_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearSection_SourceSectionTemplate'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearSection
                    ADD CONSTRAINT FK_Academics_SchoolYearSection_SourceSectionTemplate
                        FOREIGN KEY (SourceSectionTemplateId)
                        REFERENCES Academics_SectionTemplate(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearSection_AdviserTeacher'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearSection
                    ADD CONSTRAINT FK_Academics_SchoolYearSection_AdviserTeacher
                        FOREIGN KEY (AdviserTeacherId)
                        REFERENCES Academics_Teacher(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearSectionProgram_Section'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearSectionProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearSectionProgram_Section
                        FOREIGN KEY (SchoolYearSectionId)
                        REFERENCES Academics_SchoolYearSection(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearSectionProgram_Program'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearSectionProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearSectionProgram_Program
                        FOREIGN KEY (SchoolYearProgramId)
                        REFERENCES Academics_SchoolYearProgram(Id);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_Term'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_Term
                        FOREIGN KEY (TermId)
                        REFERENCES Academics_SchoolYearTerm(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_Section'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_Section
                        FOREIGN KEY (SectionId)
                        REFERENCES Academics_SchoolYearSection(Id);
                END

                IF OBJECT_ID('LearningArea', 'U') IS NOT NULL
                   AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_LearningArea'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_LearningArea
                        FOREIGN KEY (LearningAreaId)
                        REFERENCES LearningArea(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_SchoolYearProgramLine'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_SchoolYearProgramLine
                        FOREIGN KEY (SchoolYearProgramLineId)
                        REFERENCES Academics_SchoolYearProgramLine(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Subject_Teacher'
                )
                BEGIN
                    ALTER TABLE Academics_Subject
                    ADD CONSTRAINT FK_Academics_Subject_Teacher
                        FOREIGN KEY (TeacherId)
                        REFERENCES Academics_Teacher(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SubjectScheduleSlot_Subject'
                )
                BEGIN
                    ALTER TABLE Academics_SubjectScheduleSlot
                    ADD CONSTRAINT FK_Academics_SubjectScheduleSlot_Subject
                        FOREIGN KEY (SubjectId)
                        REFERENCES Academics_Subject(Id);
                END

                                IF OBJECT_ID('Learners_Learner', 'U') IS NOT NULL
                   AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Enrollment_Learner'
                )
                BEGIN
                    ALTER TABLE Academics_Enrollment
                    ADD CONSTRAINT FK_Academics_Enrollment_Learner
                        FOREIGN KEY (LearnerId)
                        REFERENCES Learners_Learner(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Enrollment_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_Enrollment
                    ADD CONSTRAINT FK_Academics_Enrollment_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Enrollment_Program'
                )
                BEGIN
                    ALTER TABLE Academics_Enrollment
                    ADD CONSTRAINT FK_Academics_Enrollment_Program
                        FOREIGN KEY (SchoolYearProgramId)
                        REFERENCES Academics_SchoolYearProgram(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Enrollment_Section'
                )
                BEGIN
                    ALTER TABLE Academics_Enrollment
                    ADD CONSTRAINT FK_Academics_Enrollment_Section
                        FOREIGN KEY (SchoolYearSectionId)
                        REFERENCES Academics_SchoolYearSection(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_Enrollment_Status'
                )
                BEGIN
                    ALTER TABLE Academics_Enrollment
                    ADD CONSTRAINT FK_Academics_Enrollment_Status
                        FOREIGN KEY (EnrollmentStatusId)
                        REFERENCES Academics_EnrollmentStatus(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_EnrollmentSubject_Enrollment'
                )
                BEGIN
                    ALTER TABLE Academics_EnrollmentSubject
                    ADD CONSTRAINT FK_Academics_EnrollmentSubject_Enrollment
                        FOREIGN KEY (EnrollmentId)
                        REFERENCES Academics_Enrollment(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_EnrollmentSubject_Subject'
                )
                BEGIN
                    ALTER TABLE Academics_EnrollmentSubject
                    ADD CONSTRAINT FK_Academics_EnrollmentSubject_Subject
                        FOREIGN KEY (SubjectId)
                        REFERENCES Academics_Subject(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_EnrollmentSubject_Status'
                )
                BEGIN
                    ALTER TABLE Academics_EnrollmentSubject
                    ADD CONSTRAINT FK_Academics_EnrollmentSubject_Status
                        FOREIGN KEY (EnrollmentStatusId)
                        REFERENCES Academics_EnrollmentStatus(Id);
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
                    WHERE name = 'UX_Academics_SchoolYearProgram_Year_SourceProgram'
                      AND object_id = OBJECT_ID('Academics_SchoolYearProgram')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYearProgram_Year_SourceProgram
                    ON Academics_SchoolYearProgram(SchoolYearId, SourceProgramId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearProgram_SchoolYear'
                      AND object_id = OBJECT_ID('Academics_SchoolYearProgram')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearProgram_SchoolYear
                    ON Academics_SchoolYearProgram
                    (
                        SchoolYearId,
                        SortOrder,
                        ProgramName
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearProgramLine_Program'
                      AND object_id = OBJECT_ID('Academics_SchoolYearProgramLine')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearProgramLine_Program
                    ON Academics_SchoolYearProgramLine
                    (
                        SchoolYearProgramId,
                        GradeLevelName,
                        TemplateTermSequence,
                        SortOrder
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearProgramLine_LearningArea'
                      AND object_id = OBJECT_ID('Academics_SchoolYearProgramLine')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearProgramLine_LearningArea
                    ON Academics_SchoolYearProgramLine(LearningAreaId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SchoolYearProgramLine_SourceLine'
                      AND object_id = OBJECT_ID('Academics_SchoolYearProgramLine')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYearProgramLine_SourceLine
                    ON Academics_SchoolYearProgramLine
                    (
                        SchoolYearProgramId,
                        SourceProgramProspectusLineId
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
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgram_SchoolYear'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearProgram_SchoolYear
                        FOREIGN KEY (SchoolYearId)
                        REFERENCES Academics_SchoolYear(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgram_SourceProgram'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgram
                    ADD CONSTRAINT FK_Academics_SchoolYearProgram_SourceProgram
                        FOREIGN KEY (SourceProgramId)
                        REFERENCES Academics_Program(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_SchoolYearProgram'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_SchoolYearProgram
                        FOREIGN KEY (SchoolYearProgramId)
                        REFERENCES Academics_SchoolYearProgram(Id);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_SourceProspectusLine'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_SourceProspectusLine
                        FOREIGN KEY (SourceProgramProspectusLineId)
                        REFERENCES Academics_ProgramProspectusLine(Id);
                END

                IF OBJECT_ID('LearningArea', 'U') IS NOT NULL
                   AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_Academics_SchoolYearProgramLine_LearningArea'
                )
                BEGIN
                    ALTER TABLE Academics_SchoolYearProgramLine
                    ADD CONSTRAINT FK_Academics_SchoolYearProgramLine_LearningArea
                        FOREIGN KEY (LearningAreaId)
                        REFERENCES LearningArea(Id);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SchoolYearSection_Year_Grade_Section'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSection')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYearSection_Year_Grade_Section
                    ON Academics_SchoolYearSection
                    (
                        SchoolYearId,
                        GradeLevelName,
                        SectionName
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearSection_SchoolYear'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSection')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearSection_SchoolYear
                    ON Academics_SchoolYearSection
                    (
                        SchoolYearId,
                        SortOrder,
                        GradeLevelName,
                        SectionName
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearSection_AdviserTeacher'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSection')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearSection_AdviserTeacher
                    ON Academics_SchoolYearSection(AdviserTeacherId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_SchoolYearSectionProgram_Link'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSectionProgram')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_SchoolYearSectionProgram_Link
                    ON Academics_SchoolYearSectionProgram
                    (
                        SchoolYearSectionId,
                        SchoolYearProgramId
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearSectionProgram_Section'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSectionProgram')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearSectionProgram_Section
                    ON Academics_SchoolYearSectionProgram
                    (
                        SchoolYearSectionId,
                        SortOrder
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SchoolYearSectionProgram_Program'
                      AND object_id = OBJECT_ID('Academics_SchoolYearSectionProgram')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SchoolYearSectionProgram_Program
                    ON Academics_SchoolYearSectionProgram(SchoolYearProgramId);
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_Subject_Term_Section_LearningArea'
                      AND object_id = OBJECT_ID('Academics_Subject')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_Subject_Term_Section_LearningArea
                    ON Academics_Subject
                    (
                        TermId,
                        SectionId,
                        LearningAreaId
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Subject_SchoolYear'
                      AND object_id = OBJECT_ID('Academics_Subject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Subject_SchoolYear
                    ON Academics_Subject
                    (
                        SchoolYearId,
                        TermId,
                        SectionId,
                        SortOrder
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Subject_Teacher'
                      AND object_id = OBJECT_ID('Academics_Subject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Subject_Teacher
                    ON Academics_Subject(TeacherId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Subject_LearningArea'
                      AND object_id = OBJECT_ID('Academics_Subject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Subject_LearningArea
                    ON Academics_Subject(LearningAreaId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Subject_ProgramLine'
                      AND object_id = OBJECT_ID('Academics_Subject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Subject_ProgramLine
                    ON Academics_Subject(SchoolYearProgramLineId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_SubjectScheduleSlot_Subject'
                      AND object_id = OBJECT_ID('Academics_SubjectScheduleSlot')
                )
                BEGIN
                    CREATE INDEX IX_Academics_SubjectScheduleSlot_Subject
                    ON Academics_SubjectScheduleSlot
                    (
                        SubjectId,
                        SortOrder,
                        DayOfWeekNumber,
                        StartTime
                    );
                END

                                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Enrollment_Learner'
                      AND object_id = OBJECT_ID('Academics_Enrollment')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Enrollment_Learner
                    ON Academics_Enrollment
                    (
                        LearnerId,
                        SchoolYearId,
                        IsCurrent,
                        IsActive
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Enrollment_SchoolYear'
                      AND object_id = OBJECT_ID('Academics_Enrollment')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Enrollment_SchoolYear
                    ON Academics_Enrollment
                    (
                        SchoolYearId,
                        SchoolYearSectionId,
                        SchoolYearProgramId,
                        GradeLevelName
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_Enrollment_CurrentLearnerSchoolYear'
                      AND object_id = OBJECT_ID('Academics_Enrollment')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_Enrollment_CurrentLearnerSchoolYear
                    ON Academics_Enrollment
                    (
                        LearnerId,
                        SchoolYearId
                    )
                    WHERE IsCurrent = 1
                      AND IsActive = 1;
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_Enrollment_Status'
                      AND object_id = OBJECT_ID('Academics_Enrollment')
                )
                BEGIN
                    CREATE INDEX IX_Academics_Enrollment_Status
                    ON Academics_Enrollment(EnrollmentStatusId);
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'UX_Academics_EnrollmentSubject_Link'
                      AND object_id = OBJECT_ID('Academics_EnrollmentSubject')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Academics_EnrollmentSubject_Link
                    ON Academics_EnrollmentSubject
                    (
                        EnrollmentId,
                        SubjectId
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_EnrollmentSubject_Enrollment'
                      AND object_id = OBJECT_ID('Academics_EnrollmentSubject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_EnrollmentSubject_Enrollment
                    ON Academics_EnrollmentSubject
                    (
                        EnrollmentId,
                        IsActive
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_EnrollmentSubject_Subject'
                      AND object_id = OBJECT_ID('Academics_EnrollmentSubject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_EnrollmentSubject_Subject
                    ON Academics_EnrollmentSubject
                    (
                        SubjectId,
                        IsActive
                    );
                END

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Academics_EnrollmentSubject_Status'
                      AND object_id = OBJECT_ID('Academics_EnrollmentSubject')
                )
                BEGIN
                    CREATE INDEX IX_Academics_EnrollmentSubject_Status
                    ON Academics_EnrollmentSubject(EnrollmentStatusId);
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
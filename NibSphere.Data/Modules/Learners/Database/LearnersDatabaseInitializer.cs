using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Database
{
	public sealed class LearnersDatabaseInitializer : IModuleDatabaseInitializer
	{
		public string ModuleKey => "learners";

		public int SortOrder => 200;

		public async Task InitializeAsync(
			IAppPaths appPaths,
			CancellationToken cancellationToken = default)
		{
			LocalDbConnectionFactory connectionFactory = new(appPaths);

			using SqlConnection connection = connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			string sql =
				"""
				IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Learners_Learner')
				BEGIN
				    CREATE TABLE Learners_Learner
				    (
				        Id INT PRIMARY KEY IDENTITY(1,1),

				        FirstName NVARCHAR(100) NOT NULL,
				        MiddleName NVARCHAR(100) NULL,
				        LastName NVARCHAR(100) NOT NULL,
				        ExtensionName NVARCHAR(50) NULL,

				        Birthday DATE NULL,

				        Sex NVARCHAR(20) NULL,
				        Pronoun NVARCHAR(50) NULL,
				        Lrn NVARCHAR(50) NULL,
				        ReligiousAffiliation NVARCHAR(100) NULL,

				        HouseStreetSitioPurok NVARCHAR(300) NULL,
				        Barangay NVARCHAR(150) NULL,
				        Municipality NVARCHAR(150) NULL,
				        Province NVARCHAR(150) NULL,

				        MobileNumber NVARCHAR(50) NULL,
				        Email NVARCHAR(150) NULL,

				        ProfilePicturePath NVARCHAR(500) NULL,

				        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
				        UpdatedAt DATETIME2 NULL
				    );
				END

				IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Learners_Custodian')
				BEGIN
				    CREATE TABLE Learners_Custodian
				    (
				        Id INT PRIMARY KEY IDENTITY(1,1),

				        FirstName NVARCHAR(100) NOT NULL,
				        MiddleName NVARCHAR(100) NULL,
				        LastName NVARCHAR(100) NOT NULL,
				        ExtensionName NVARCHAR(50) NULL,

				        MobileNumber NVARCHAR(50) NULL,
				        Email NVARCHAR(150) NULL,

				        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
				        UpdatedAt DATETIME2 NULL
				    );
				END

				IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Learners_CustodianRole')
				BEGIN
				    CREATE TABLE Learners_CustodianRole
				    (
				        Id INT PRIMARY KEY IDENTITY(1,1),

				        RelationshipType NVARCHAR(50) NOT NULL,
				        RelationshipLabel NVARCHAR(100) NOT NULL,
				        SortOrder INT NOT NULL DEFAULT 0,

				        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
				        UpdatedAt DATETIME2 NULL
				    );
				END

				IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Learners_LearnerCustodian')
				BEGIN
				    CREATE TABLE Learners_LearnerCustodian
				    (
				        Id INT PRIMARY KEY IDENTITY(1,1),

				        LearnerId INT NOT NULL,
				        CustodianId INT NOT NULL,

				        RelationshipType NVARCHAR(50) NOT NULL,
				        RelationshipLabel NVARCHAR(100) NOT NULL,

				        HasCustody BIT NOT NULL DEFAULT 0,
				        LivesWithLearner BIT NOT NULL DEFAULT 0,

				        SortOrder INT NOT NULL DEFAULT 0,

				        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
				        UpdatedAt DATETIME2 NULL
				    );
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.foreign_keys
				    WHERE name = 'FK_Learners_LearnerCustodian_Learner'
				)
				BEGIN
				    ALTER TABLE Learners_LearnerCustodian
				    ADD CONSTRAINT FK_Learners_LearnerCustodian_Learner
				        FOREIGN KEY (LearnerId)
				        REFERENCES Learners_Learner(Id);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.foreign_keys
				    WHERE name = 'FK_Learners_LearnerCustodian_Custodian'
				)
				BEGIN
				    ALTER TABLE Learners_LearnerCustodian
				    ADD CONSTRAINT FK_Learners_LearnerCustodian_Custodian
				        FOREIGN KEY (CustodianId)
				        REFERENCES Learners_Custodian(Id);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'IX_Learners_Learner_Name'
				      AND object_id = OBJECT_ID('Learners_Learner')
				)
				BEGIN
				    CREATE INDEX IX_Learners_Learner_Name
				    ON Learners_Learner(LastName, FirstName, MiddleName);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'IX_Learners_Learner_Lrn'
				      AND object_id = OBJECT_ID('Learners_Learner')
				)
				BEGIN
				    CREATE INDEX IX_Learners_Learner_Lrn
				    ON Learners_Learner(Lrn);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'IX_Learners_Custodian_Name'
				      AND object_id = OBJECT_ID('Learners_Custodian')
				)
				BEGIN
				    CREATE INDEX IX_Learners_Custodian_Name
				    ON Learners_Custodian(LastName, FirstName, MiddleName);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'IX_Learners_LearnerCustodian_LearnerId'
				      AND object_id = OBJECT_ID('Learners_LearnerCustodian')
				)
				BEGIN
				    CREATE INDEX IX_Learners_LearnerCustodian_LearnerId
				    ON Learners_LearnerCustodian(LearnerId);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'IX_Learners_LearnerCustodian_CustodianId'
				      AND object_id = OBJECT_ID('Learners_LearnerCustodian')
				)
				BEGIN
				    CREATE INDEX IX_Learners_LearnerCustodian_CustodianId
				    ON Learners_LearnerCustodian(CustodianId);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'UX_Learners_LearnerCustodian_Link'
				      AND object_id = OBJECT_ID('Learners_LearnerCustodian')
				)
				BEGIN
				    CREATE UNIQUE INDEX UX_Learners_LearnerCustodian_Link
				    ON Learners_LearnerCustodian
				    (
				        LearnerId,
				        CustodianId,
				        RelationshipType,
				        RelationshipLabel
				    );
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM sys.indexes
				    WHERE name = 'UX_Learners_CustodianRole_TypeLabel'
				      AND object_id = OBJECT_ID('Learners_CustodianRole')
				)
				BEGIN
				    CREATE UNIQUE INDEX UX_Learners_CustodianRole_TypeLabel
				    ON Learners_CustodianRole(RelationshipType, RelationshipLabel);
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM Learners_CustodianRole
				    WHERE RelationshipType = 'Parent'
				      AND RelationshipLabel = 'Father'
				)
				BEGIN
				    INSERT INTO Learners_CustodianRole
				    (
				        RelationshipType,
				        RelationshipLabel,
				        SortOrder
				    )
				    VALUES
				    (
				        'Parent',
				        'Father',
				        1
				    );
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM Learners_CustodianRole
				    WHERE RelationshipType = 'Parent'
				      AND RelationshipLabel = 'Mother'
				)
				BEGIN
				    INSERT INTO Learners_CustodianRole
				    (
				        RelationshipType,
				        RelationshipLabel,
				        SortOrder
				    )
				    VALUES
				    (
				        'Parent',
				        'Mother',
				        2
				    );
				END

				IF NOT EXISTS
				(
				    SELECT 1
				    FROM Learners_CustodianRole
				    WHERE RelationshipType = 'Guardian'
				      AND RelationshipLabel = 'Guardian'
				)
				BEGIN
				    INSERT INTO Learners_CustodianRole
				    (
				        RelationshipType,
				        RelationshipLabel,
				        SortOrder
				    )
				    VALUES
				    (
				        'Guardian',
				        'Guardian',
				        3
				    );
				END
				""";

			using SqlCommand command = new(sql, connection);
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}
}
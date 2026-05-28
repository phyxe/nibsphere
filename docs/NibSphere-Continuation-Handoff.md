# NibSphere Continuation Handoff

This document preserves the current development direction for **NibSphere** so that a future conversation can continue the project without confusing it with the planned teacher-local app.

## 1. Product Identity

NibSphere should continue as the broader **school-management-system-oriented** project.

Its long-term direction is not limited to a single teacher. It is allowed to grow into a system that can support school-year setup, programs, prospectus structures, section management, enrollment, subject offerings, teacher assignment, scheduling metadata, reporting, and eventually multi-instance or API-based coordination.

A separate future project may be created for the simplified local teacher app. That teacher-local app should focus on class advisers and subject teachers using a smaller spine: school information, school years, students, subjects, class groups, gradebooks, advisory subject clusters, and reports. That direction should not replace NibSphere’s broader purpose.

## 2. Current Architectural Position

NibSphere currently uses a multi-project structure:

- `NibSphere` for the WPF shell and UI.
- `NibSphere.Core` for shared models and interfaces.
- `NibSphere.Data` for LocalDB persistence, repositories, and schema initialization.

This structure remains appropriate for NibSphere because the project is trending toward a broader school-management system rather than a small single-purpose teacher tool.

## 3. Startup and Theme Work Already Done

The app previously had visible startup delay and theme flicker issues. A cached theme preference was added to reduce light-to-dark flicker risk once the shell is later shown earlier.

Current startup idea:

1. Create app paths and module catalog.
2. Ensure storage and reference data directories/files.
3. Read cached theme preference from the config folder.
4. Apply the cached theme before database work.
5. Initialize the core database and module databases.
6. Reconcile theme preference from the database.
7. Start the WPF application.

Relevant design rule:

- The first visible shell should eventually open using the best-known cached theme.
- The database-backed user profile remains the canonical saved user preference.
- The config theme cache exists to avoid startup flicker and reduce early dependence on LocalDB.

## 4. Schema Versioning Work Already Done

A schema version marker was added so heavy database initialization does not run every startup.

Key components:

- `DatabaseSchemaVersionService`
- `AppSchemaVersion` table
- core schema key: `core`
- module schema keys such as `module:academics`

Development rule:

- When changing core tables, bump the core schema version.
- When changing module tables, indexes, foreign keys, or seed data, bump that module’s `SchemaVersion`.
- If a schema migration was already marked as applied locally, either delete the local development database or bump the module schema version again.

## 5. Current Academics Schema Direction

The Academics module was adjusted so that it can support multiple future setup paths.

Originally, the design was moving strongly toward:

```text
Master Program Prospectus
-> School-Year Program Deployment
-> School-Year Program Lines
-> Subject Offerings
-> Enrollment
-> EnrollmentSubject links
```

That path is still valid for the school-management direction, but it should not be the only possible internal path.

Current design principle:

```text
Program/prospectus/deployment can exist as school-level infrastructure,
but enrollment and subject offerings should not be permanently locked to master-program deployment.
```

## 6. Important Recent Schema Changes

The Academics module is currently at `SchemaVersion => 3`.

### 6.1 School-Year Program Lines

`Academics_SchoolYearProgramLine` was loosened to support both master-derived and local/custom school-year lines.

Important fields:

- `SourceProgramProspectusLineId INT NULL`
- `LineOrigin NVARCHAR(50) NOT NULL DEFAULT N'MasterProspectus'`
- `SnapshotNotes NVARCHAR(500) NULL`

Purpose:

- A school-year line can come from a master prospectus.
- A school-year line can also later come from adviser setup, manual school-year setup, historical record reconstruction, or import.
- Changes to school-year snapshots should not write back to the master prospectus.

Recommended future `LineOrigin` values:

```text
MasterProspectus
AdviserSetup
ManualSchoolYear
HistoricalRecord
Imported
```

### 6.2 Enrollment

`Academics_Enrollment` was loosened so program context is optional.

Important fields:

- `SchoolYearProgramId INT NULL`
- `EnrollmentOrigin NVARCHAR(50) NOT NULL DEFAULT N'SchoolAdmin'`
- `EnrollmentScope NVARCHAR(50) NOT NULL DEFAULT N'ProgramSection'`

Purpose:

- Class adviser workflows can use program/section context.
- Subject-teacher or roster-like workflows can work without a deployed program.
- Enrollment can function as the internal learner-membership record for a school-year class, advisory section, teaching group, or other future context.

Recommended future `EnrollmentOrigin` values:

```text
SchoolAdmin
AdviserSetup
SubjectTeacherSetup
HistoricalRecord
Imported
```

Recommended future `EnrollmentScope` values:

```text
ProgramSection
AdvisorySection
SubjectRoster
HistoricalRecord
```

### 6.3 Current Enrollment Uniqueness

The active-current enrollment uniqueness rule was changed.

Old behavior:

```text
One current active enrollment per learner per school year.
```

New intended behavior:

```text
One current active enrollment per learner, school year, section, and enrollment scope.
```

The corrected unique index should use:

```text
LearnerId
SchoolYearId
SchoolYearSectionId
EnrollmentScope
WHERE IsCurrent = 1 AND IsActive = 1
```

This prevents one learner from being accidentally removed from another valid context in the same school year, such as another subject roster or teaching group.

## 7. Current Repository/Model Expectations

### 7.1 `AcademicsSchoolYearProgramLine`

The model should include:

- nullable `SourceProgramProspectusLineId`
- `LineOrigin`
- `SnapshotNotes`
- helper property `IsFromMasterProspectus`

### 7.2 `AcademicsEnrollment`

The model should include:

- nullable `SchoolYearProgramId`
- `EnrollmentOrigin`
- `EnrollmentScope`
- helper property `HasProgramContext`

### 7.3 `AcademicsSchoolYearProgramRepository`

`DeployProgramAsync(...)` should continue to copy only active prospectus lines.

When it inserts copied lines, it should set:

```text
LineOrigin = 'MasterProspectus'
SnapshotNotes = NULL
```

It should not write back to the master program prospectus.

### 7.4 `AcademicsEnrollmentRepository`

Enrollment queries should use a `LEFT JOIN` to `Academics_SchoolYearProgram` because `SchoolYearProgramId` is now nullable.

Insert/update methods should clear current records only within:

```text
LearnerId
SchoolYearId
SchoolYearSectionId
EnrollmentScope
```

not by learner and school year alone.

## 8. Conceptual Role Boundaries in NibSphere

NibSphere can later support several user-facing areas or licensed workflows, but the shared academic records should remain centralized.

Possible future layers:

```text
Academics Foundation
- school years
- terms
- programs
- prospectus lines
- school-year program snapshots
- sections
- subject offerings
- enrollment
- enrollment-subject links

School Admin
- full school-year setup
- program/prospectus management
- deployment
- section and program linking
- subject offering generation
- teacher assignment
- schedule metadata
- enrollment management
- bulk tools

Class Adviser
- advisory class records
- learner-level reports
- attendance
- behavior
- consolidated term grades
- report cards
- permanent records/transcript support

Subject Teacher
- subject-level class lists
- gradebooks
- assessment tracking
- term-grade computation
- subject reports

Reports
- cross-cutting output layer that can read from all relevant records
```

Important rule:

```text
License or expose workflows, not canonical records.
```

The underlying academic records should remain shared so adviser and subject-teacher features do not duplicate learners, subjects, or class membership.

## 9. Product Split Decision

During planning, the direction split into two possible products:

### 9.1 NibSphere

NibSphere remains the broader school-management-system project.

It can keep:

- program/prospectus setup
- school-year deployment
- enrollment infrastructure
- subject offerings
- teacher/schedule metadata
- school admin concepts
- future API/multi-instance potential

### 9.2 Future Teacher-Local App

A separate new project, not necessarily a fork, may be created later for the local teacher app.

That future app should probably be a single WPF project, not a three-project solution like NibSphere.

Suggested teacher-local app structure:

```text
Main container / app settings
Class Adviser feature
Subject Teacher feature
Reports feature
Shared setup spine
```

Suggested teacher-local app user-facing spine:

```text
School information
School years and grading terms
Student list
Subject list
Statuses
Class groups
Gradebooks
Advisory subject clusters
Reports
```

The teacher-local app should not require program deployment or enrollment management as user-facing concepts.

This future teacher-local project should not derail NibSphere’s current school-management-system path.

## 10. Immediate NibSphere Development Status

The last completed work before this handoff:

1. Cached theme preference for startup theme stability.
2. Schema versioning service and version table.
3. Academics schema versioning through module schema versions.
4. School-year program lines loosened for nullable source prospectus lines and line origin metadata.
5. Enrollment loosened for nullable school-year program and enrollment origin/scope metadata.
6. Enrollment active-current uniqueness scoped by learner, school year, section, and enrollment scope.

## 11. Recommended Next NibSphere Slice

The next NibSphere slice should probably be **subject offering foundation**, unless the developer decides to pause NibSphere and begin the separate teacher-local app.

For NibSphere, subject offering should be treated as the operational bridge from school-year structure to gradebook/reporting.

Current table already acting as subject offering:

```text
Academics_Subject
```

It contains:

- `SchoolYearId`
- `TermId`
- `SectionId`
- `LearningAreaId`
- optional `SchoolYearProgramLineId`
- optional teacher metadata
- subject code/name
- sort order
- active flag

Next possible backend tasks:

1. Add repository methods for subject offering options and creation.
2. Support creation from a school-year program line.
3. Support direct/manual creation without a school-year program line.
4. Preserve optional teacher assignment metadata.
5. Preserve optional schedule metadata through `Academics_SubjectScheduleSlot`.
6. Add methods to list subject offerings by school year, term, section, teacher, and learning area.
7. Later, add enrollment-subject sync methods.

Suggested behavior:

```text
School Admin path:
Master prospectus -> school-year snapshot -> generated subject offerings

Adviser path:
School-year/advisory snapshot -> subject offerings for advisory reporting

Subject Teacher path:
Direct subject offering -> roster/gradebook
```

## 12. Development Cautions

Do not prematurely remove program/prospectus/deployment code from NibSphere. That code fits NibSphere’s broader school-management-system direction.

Do not force all workflows through program deployment. The schema was deliberately loosened so future workflows can create subject offerings and enrollment-like records without a master prospectus.

Do not expose all internal academic infrastructure to ordinary users at once. A later setup wizard or workflow-specific UI can hide complexity.

Do not merge the teacher-local simplified product direction back into NibSphere unless a deliberate architectural decision is made.

## 13. Local Development Notes

Because schema versioning now skips applied migrations, remember:

- If the local database has already recorded a schema version, corrected migration SQL will not rerun automatically.
- During development, either delete the local database or bump the relevant schema version.
- For the most recent migration correction, if the app had already recorded `module:academics = 3` before the corrected SQL was pushed, the developer should delete the local database or temporarily bump Academics schema version again.

## 14. Working Summary for a New Conversation

If a new conversation continues NibSphere, start from this understanding:

```text
NibSphere is the broader school-management-system codebase.
The teacher-local app is a separate future project.
NibSphere should keep program/prospectus/deployment infrastructure.
The Academics schema has been loosened so deployment is not the only path.
Next likely NibSphere task: subject offering foundation and repository methods.
```

The current development posture is to preserve flexibility:

```text
Master-derived school-year structures are allowed.
Manual/local school-year structures are allowed.
Program-based enrollments are allowed.
Roster-like enrollments without program context are allowed.
Subject offerings should become the bridge to gradebook and reporting.
```

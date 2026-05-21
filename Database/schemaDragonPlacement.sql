CREATE TABLE Dragon (
	DragonId INTEGER NOT NULL,
	GivenName TEXT NOT NULL,
	FamilyName TEXT,
	CanBreathFire INTEGER DEFAULT (0) NOT NULL,
	CanTakePassengers INTEGER DEFAULT (0) NOT NULL,
	WeightInKg NUMERIC,
	LengthInMeters NUMERIC, FightingSkills TEXT,
	CONSTRAINT PK_Dragon PRIMARY KEY (DragonId)
);
CREATE TABLE Job (
	JobId INTEGER NOT NULL,
	JobTitle TEXT NOT NULL,
	EmployerName TEXT,
	NumberOfPositions INTEGER DEFAULT (1) NOT NULL,
	StartDateUnix INTEGER NOT NULL,
	EndDateUnix INTEGER NOT NULL,
	CONSTRAINT PK_Job PRIMARY KEY (JobId)
);
CREATE TABLE IF NOT EXISTS "Assignment" (
    AssignmentId INTEGER NOT NULL,
    DragonId     INTEGER NOT NULL,
    JobId        INTEGER NOT NULL,
    StartDateUnix INTEGER NOT NULL,
    EndDateUnix  INTEGER NOT NULL,
    CONSTRAINT PK_Assignments PRIMARY KEY (AssignmentId),
    CONSTRAINT FK_Assignments_Dragon FOREIGN KEY (DragonId) REFERENCES Dragon(DragonId),
    CONSTRAINT FK_Assignments_Job    FOREIGN KEY (JobId)    REFERENCES Job(JobId)
);
CREATE UNIQUE INDEX Assignment_UK_JobId_DragonId ON Assignment(JobId, DragonId);
CREATE TABLE SkillTag (
    SkillTagId INTEGER NOT NULL,
    SkillName TEXT NOT NULL,
    CONSTRAINT PK_SkillTag PRIMARY KEY (SkillTagId)
);
CREATE TABLE DragonSkillTag (
    DragonId   INTEGER NOT NULL,
    SkillTagId INTEGER NOT NULL,
    CONSTRAINT PK_DragonSkillTag PRIMARY KEY (DragonId, SkillTagId),
    CONSTRAINT FK_DragonSkillTag_Dragon   FOREIGN KEY (DragonId)   REFERENCES Dragon(DragonId),
    CONSTRAINT FK_DragonSkillTag_SkillTag FOREIGN KEY (SkillTagId) REFERENCES SkillTag(SkillTagId)
);
CREATE TABLE JobSkillTag (
    JobId      INTEGER NOT NULL,
    SkillTagId INTEGER NOT NULL,
    CONSTRAINT PK_JobSkillTag PRIMARY KEY (JobId, SkillTagId),
    CONSTRAINT FK_JobSkillTag_Job      FOREIGN KEY (JobId)      REFERENCES Job(JobId),
    CONSTRAINT FK_JobSkillTag_SkillTag FOREIGN KEY (SkillTagId) REFERENCES SkillTag(SkillTagId)
);

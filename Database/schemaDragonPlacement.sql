CREATE TABLE Dragon (
	DragonId INTEGER NOT NULL,
	GivenName TEXT NOT NULL,
	FamilyName TEXT,
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
CREATE TABLE IF NOT EXISTS "HoursWorked" (
    HoursWorkedId     INTEGER NOT NULL,
    StartDateTimeUnix INTEGER NOT NULL,
    EndDateTimeUnix   INTEGER NOT NULL,
    PayPeriodId       INTEGER NOT NULL,
    CONSTRAINT PK_HoursWorkedId PRIMARY KEY (HoursWorkedId),
    CONSTRAINT FK_PayPeriodId FOREIGN KEY (PayPeriodId) REFERENCES PayPeriod(PayPeriodId)
);
CREATE TABLE PayPeriod (
	PayPeriodId INTEGER NOT NULL,
	AssignmentId INTEGER NOT NULL,
	StartDateUnix INTEGER NOT NULL,
	EndDateUnix INTEGER NOT NULL,
	SubmissionStatus TEXT NOT NULL,
	CONSTRAINT PK_PayPeriodId PRIMARY KEY (PayPeriodId),
	CONSTRAINT FK_AssignmentId FOREIGN KEY (AssignmentId) REFERENCES "Assignment"(AssignmentId)
);
CREATE TABLE IF NOT EXISTS "ChargeRate" (
    ChargeRateId INTEGER NOT NULL,
    AssignmentId INTEGER NOT NULL,
    HourlyRate   NUMERIC NOT NULL,
    CONSTRAINT PK_ChargeRate PRIMARY KEY (ChargeRateId),
    CONSTRAINT FK_ChargeRate_Assignment FOREIGN KEY (AssignmentId) REFERENCES "Assignment"(AssignmentId)
);
CREATE TABLE IF NOT EXISTS "BillableHours" (
    BillableHoursId INTEGER NOT NULL,
    ChargeRateId    INTEGER NOT NULL,
    PayPeriodId     INTEGER NOT NULL,
    HourlyRate      NUMERIC NOT NULL,
    TotalHours      NUMERIC NOT NULL,
    BillingStatus   TEXT    NOT NULL,
    CONSTRAINT PK_BillableHours PRIMARY KEY (BillableHoursId),
    CONSTRAINT FK_BillableHours_ChargeRate FOREIGN KEY (ChargeRateId) REFERENCES "ChargeRate"(ChargeRateId),
    CONSTRAINT FK_BillableHours_PayPeriod FOREIGN KEY (PayPeriodId) REFERENCES "PayPeriod"
);

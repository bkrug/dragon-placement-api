CREATE TABLE Dragon (
	DragonId INTEGER NOT NULL,
	GivenName TEXT NOT NULL,
	FamilyName TEXT,
	CanBreathFire INTEGER DEFAULT (0) NOT NULL,
	CanTakePassengers INTEGER DEFAULT (0) NOT NULL,
	Weight NUMERIC,
	LengthInMeters NUMERIC,
	CONSTRAINT PK_PokemonWorker PRIMARY KEY (DragonId)
);

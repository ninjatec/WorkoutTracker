ALTER TABLE WorkoutSets ADD
  Status NVARCHAR(50) NULL,
  IsWarmup BIT NOT NULL DEFAULT 0,
  RestTime TIME NULL,
  TargetReps INT NULL,
  TargetWeight DECIMAL(18,2) NULL,
  EstimatedOneRM DECIMAL(18,2) NULL;

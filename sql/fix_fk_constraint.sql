-- Fix for FK_WorkoutSessions_User_UserId1 constraint issue
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkoutSessions_User_UserId1')
BEGIN
    ALTER TABLE WorkoutSessions DROP CONSTRAINT FK_WorkoutSessions_User_UserId1;
    PRINT 'Constraint FK_WorkoutSessions_User_UserId1 dropped successfully';
END
ELSE
BEGIN
    PRINT 'Constraint FK_WorkoutSessions_User_UserId1 does not exist, no action taken';
END

-- Update the UserId foreign key to have Restrict delete behavior
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkoutSessions_User_UserId')
BEGIN
    ALTER TABLE WorkoutSessions DROP CONSTRAINT FK_WorkoutSessions_User_UserId;
    PRINT 'Dropped existing UserId foreign key';
END

-- Add the foreign key back with RESTRICT delete behavior
ALTER TABLE WorkoutSessions
ADD CONSTRAINT FK_WorkoutSessions_User_UserId 
FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE NO ACTION;
PRINT 'Re-added UserId foreign key with NO ACTION delete behavior'

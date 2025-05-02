IF OBJECT_ID('FK_WorkoutSessions_User_UserId1', 'F') IS NOT NULL
    ALTER TABLE WorkoutSessions DROP CONSTRAINT FK_WorkoutSessions_User_UserId1;
GO

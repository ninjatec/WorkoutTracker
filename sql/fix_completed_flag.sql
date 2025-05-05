-- SQL script to update IsCompleted flag for TrainAI imported workout sets
-- This will mark all existing sets as completed (successful)
-- Replace WorkoutSets with the actual table name if different

SELECT COUNT(*) AS 'Total Sets', 
       SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) AS 'Currently Completed',
       SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END) AS 'Currently Failed'
FROM WorkoutSets;

-- Uncomment the below UPDATE statement after confirming the counts
-- UPDATE WorkoutSets 
-- SET IsCompleted = 1
-- WHERE EXISTS (
--    SELECT 1 
--    FROM WorkoutExercises we
--    JOIN WorkoutSessions ws ON we.WorkoutSessionId = ws.WorkoutSessionId
--    WHERE we.WorkoutExerciseId = WorkoutSets.WorkoutExerciseId
--      AND ws.Status = 'Completed'
-- );

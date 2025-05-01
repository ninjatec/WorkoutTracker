USE WorkoutTrackerWeb;

-- Disable constraints
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"

-- Clear existing data to ensure clean migration
DELETE FROM WorkoutSets;
DELETE FROM WorkoutExercises;
DELETE FROM WorkoutSessions;

-- Create WorkoutSession records for Sessions
INSERT INTO WorkoutSessions (
    Name, 
    Description, 
    UserId, 
    StartDateTime, 
    EndDateTime,
    Status, 
    SessionId,
    Duration,
    IsCompleted,
    IsFromCoach
)
SELECT 
    s.Name, 
    s.Notes, 
    s.UserId, 
    s.datetime,
    s.endtime,
    CASE 
        WHEN s.endtime IS NOT NULL THEN 'Completed'
        ELSE 'Created'
    END,
    s.SessionId,
    CASE
        WHEN s.endtime IS NOT NULL THEN DATEDIFF(MINUTE, s.datetime, s.endtime)
        ELSE 0
    END,
    CASE
        WHEN s.endtime IS NOT NULL THEN 1
        ELSE 0
    END,
    0
FROM Session s;

-- Create WorkoutExercises from Sets
INSERT INTO WorkoutExercises (
    WorkoutSessionId, 
    ExerciseTypeId, 
    SequenceNum, 
    OrderIndex
)
SELECT DISTINCT
    ws.WorkoutSessionId,
    s.ExerciseTypeId,
    MIN(s.SequenceNum) AS SequenceNum,
    MIN(s.SequenceNum) AS OrderIndex
FROM Session sess
JOIN WorkoutSessions ws ON ws.SessionId = sess.SessionId
JOIN [Set] s ON s.SessionId = sess.SessionId
GROUP BY ws.WorkoutSessionId, s.ExerciseTypeId;

-- Create WorkoutSets from Sets
INSERT INTO WorkoutSets (
    WorkoutExerciseId,
    SettypeId,
    SequenceNum,
    SetNumber,
    Reps,
    Weight,
    Notes,
    Timestamp,
    IsCompleted
)
SELECT 
    we.WorkoutExerciseId,
    s.SettypeId,
    s.SequenceNum,
    ROW_NUMBER() OVER (PARTITION BY we.WorkoutExerciseId ORDER BY s.SequenceNum),
    s.NumberReps,
    s.Weight,
    s.Notes,
    sess.datetime,
    CASE 
        WHEN sess.endtime IS NOT NULL THEN 1 
        ELSE 0 
    END
FROM [Set] s
JOIN Session sess ON s.SessionId = sess.SessionId
JOIN WorkoutSessions ws ON ws.SessionId = sess.SessionId
JOIN WorkoutExercises we ON we.WorkoutSessionId = ws.WorkoutSessionId 
                        AND we.ExerciseTypeId = s.ExerciseTypeId;

-- Update ShareToken to reference WorkoutSession
UPDATE st
SET st.WorkoutSessionId = ws.WorkoutSessionId
FROM ShareToken st
JOIN WorkoutSessions ws ON ws.SessionId = st.SessionId
WHERE st.SessionId IS NOT NULL;

-- Re-enable constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"

-- Print migration statistics
SELECT 'WorkoutSessions created' AS Metric, COUNT(*) AS Count FROM WorkoutSessions
UNION ALL
SELECT 'WorkoutExercises created', COUNT(*) FROM WorkoutExercises
UNION ALL
SELECT 'WorkoutSets created', COUNT(*) FROM WorkoutSets
UNION ALL
SELECT 'ShareTokens updated', COUNT(*) FROM ShareToken WHERE WorkoutSessionId IS NOT NULL;

-- Verify data integrity
SELECT 'Sessions without WorkoutSessions' AS Issue, COUNT(*) AS Count
FROM Session s
LEFT JOIN WorkoutSessions ws ON ws.SessionId = s.SessionId
WHERE ws.WorkoutSessionId IS NULL
UNION ALL
SELECT 'Sets without WorkoutSets', COUNT(*)
FROM [Set] s
JOIN Session sess ON s.SessionId = sess.SessionId
JOIN WorkoutSessions ws ON ws.SessionId = sess.SessionId
LEFT JOIN WorkoutExercises we ON we.WorkoutSessionId = ws.WorkoutSessionId 
                            AND we.ExerciseTypeId = s.ExerciseTypeId
LEFT JOIN WorkoutSets wss ON wss.WorkoutExerciseId = we.WorkoutExerciseId
                        AND wss.SequenceNum = s.SequenceNum
WHERE wss.WorkoutSetId IS NULL;

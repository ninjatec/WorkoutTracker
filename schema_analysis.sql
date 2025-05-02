-- Script to analyze model-database schema mismatches

-- Generate a report of all EF Core model properties vs database columns
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    CASE WHEN m.TableName IS NULL THEN 'Missing from Model' 
         WHEN m.ColumnName IS NULL THEN 'Missing from DB' 
         ELSE 'OK' END AS Status
FROM 
    (SELECT DISTINCT
        object_id('__EFMigrationsHistory') AS object_id,
        'Table in DB but not referenced by EF migrations' AS reason
    UNION ALL
    SELECT DISTINCT o.object_id, 'Column in table but missing in model'
    FROM sys.objects o
    JOIN sys.columns c ON o.object_id = c.object_id
    LEFT JOIN INFORMATION_SCHEMA.COLUMNS ef ON ef.TABLE_NAME = o.name AND ef.COLUMN_NAME = c.name
    WHERE o.type = 'U'
    AND ef.COLUMN_NAME IS NULL
    ) AS discrepancies
JOIN sys.objects t ON discrepancies.object_id = t.object_id
LEFT JOIN sys.columns c ON t.object_id = c.object_id
LEFT JOIN (
    SELECT 
        TABLE_NAME AS TableName, 
        COLUMN_NAME AS ColumnName
    FROM INFORMATION_SCHEMA.COLUMNS
) m ON t.name = m.TableName AND c.name = m.ColumnName
ORDER BY t.name, c.name;

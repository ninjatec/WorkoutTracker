-- SQL Migration Script for the WorkoutMetric table
-- This script only adds the new table without trying to modify existing constraints

-- Create WorkoutMetrics table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkoutMetrics')
BEGIN
    CREATE TABLE [WorkoutMetrics] (
        [Id] [uniqueidentifier] NOT NULL,
        [UserId] [int] NOT NULL,
        [Date] [datetime2](7) NOT NULL,
        [MetricType] [nvarchar](450) NOT NULL,
        [Value] [decimal](10, 2) NOT NULL,
        [AdditionalData] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_WorkoutMetrics] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WorkoutMetrics_User] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId]) ON DELETE CASCADE
    );
    
    -- Add indexes for better performance
    CREATE INDEX [IX_WorkoutMetrics_UserId] ON [WorkoutMetrics] ([UserId]);
    CREATE INDEX [IX_WorkoutMetrics_Date] ON [WorkoutMetrics] ([Date]);
    CREATE INDEX [IX_WorkoutMetrics_MetricType] ON [WorkoutMetrics] ([MetricType]);
    CREATE UNIQUE INDEX [IX_WorkoutMetrics_UserId_Date_MetricType] ON [WorkoutMetrics] ([UserId], [Date], [MetricType]);
    
    PRINT 'WorkoutMetrics table created successfully';
END
ELSE
BEGIN
    PRINT 'WorkoutMetrics table already exists';
END
-- Hangfire SQL objects installation script for SQL Server

-- Create Hangfire schema if it doesn't exist
IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'HangFire')
BEGIN
    EXEC ('CREATE SCHEMA [HangFire]')
    PRINT 'Created schema [HangFire]'
END
ELSE
BEGIN
    PRINT 'Schema [HangFire] already exists'
END
GO

-- Create tables if they don't exist
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Job]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Job] (
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [StateId] [bigint] NULL,
        [StateName] [nvarchar](20) NULL,
        [InvocationData] [nvarchar](max) NOT NULL,
        [Arguments] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime] NOT NULL,
        [ExpireAt] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Job]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Job] already exists'
END
GO

-- Create AggregatedCounter table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[AggregatedCounter]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[AggregatedCounter] (
        [Key] [nvarchar](100) NOT NULL,
        [Value] [bigint] NOT NULL,
        [ExpireAt] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED ([Key] ASC)
    );
    PRINT 'Created table [HangFire].[AggregatedCounter]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[AggregatedCounter] already exists'
END
GO

-- Create Counter table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Counter]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Counter] (
        [Key] [nvarchar](100) NOT NULL,
        [Value] [int] NOT NULL,
        [ExpireAt] [datetime] NULL
    );
    CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key] ON [HangFire].[Counter] ([Key] ASC);
    PRINT 'Created table [HangFire].[Counter]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Counter] already exists'
END
GO

-- Create Hash table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Hash]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Hash] (
        [Key] [nvarchar](100) NOT NULL,
        [Field] [nvarchar](100) NOT NULL,
        [Value] [nvarchar](max) NULL,
        [ExpireAt] [datetime2](7) NULL,
        
        CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Key] ASC, [Field] ASC)
    );
    PRINT 'Created table [HangFire].[Hash]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Hash] already exists'
END
GO

-- Create JobParameter table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[JobParameter]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[JobParameter](
        [JobId] [bigint] NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [Value] [nvarchar](max) NULL,
        
        CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED ([JobId] ASC, [Name] ASC)
    );
    PRINT 'Created table [HangFire].[JobParameter]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[JobParameter] already exists'
END
GO

-- Create JobQueue table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[JobQueue]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[JobQueue](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [JobId] [bigint] NOT NULL,
        [Queue] [nvarchar](50) NOT NULL,
        [FetchedAt] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue] ([Queue] ASC, [FetchedAt] ASC);
    PRINT 'Created table [HangFire].[JobQueue]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[JobQueue] already exists'
END
GO

-- Create List table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[List]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[List](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Value] [nvarchar](max) NULL,
        [ExpireAt] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_HangFire_List_Key] ON [HangFire].[List] ([Key] ASC);
    PRINT 'Created table [HangFire].[List]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[List] already exists'
END
GO

-- Create Schema table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Schema]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Schema](
        [Version] [int] NOT NULL,
        
        CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created table [HangFire].[Schema]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Schema] already exists'
END
GO

-- Create Server table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Server]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Server](
        [Id] [nvarchar](200) NOT NULL,
        [Data] [nvarchar](max) NULL,
        [LastHeartbeat] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Server]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Server] already exists'
END
GO

-- Create Set table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Set]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Set](
        [Key] [nvarchar](100) NOT NULL,
        [Score] [float] NOT NULL,
        [Value] [nvarchar](256) NOT NULL,
        [ExpireAt] [datetime] NULL,
        
        CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Key] ASC, [Value] ASC)
    );
    PRINT 'Created table [HangFire].[Set]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[Set] already exists'
END
GO

-- Create State table
IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[State]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[State](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [JobId] [bigint] NOT NULL,
        [Name] [nvarchar](20) NOT NULL,
        [Reason] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL,
        [Data] [nvarchar](max) NULL,
        
        CONSTRAINT [PK_HangFire_State] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    ALTER TABLE [HangFire].[State] ADD CONSTRAINT [FK_HangFire_State_Job] FOREIGN KEY([JobId])
        REFERENCES [HangFire].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created table [HangFire].[State]'
END
ELSE
BEGIN
    PRINT 'Table [HangFire].[State] already exists'
END
GO

-- Add schema version
IF NOT EXISTS (SELECT * FROM [HangFire].[Schema] WHERE [Version] = 7)
BEGIN
    INSERT INTO [HangFire].[Schema] ([Version]) VALUES (7);
    PRINT 'Added schema version 7'
END
ELSE
BEGIN
    PRINT 'Schema version 7 already added'
END
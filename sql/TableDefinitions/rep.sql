USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[rep]    Script Date: 27/03/2025 14:53:28 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[rep](
	[repid] [int] IDENTITY(1,1) NOT NULL,
	[setID] [int] NOT NULL,
	[ExcerciseID] [int] NOT NULL,
	[weight] [numeric](4, 2) NOT NULL,
	[settype] [int] NOT NULL,
	[repnum] [int] NOT NULL,
	[success] [bit] NOT NULL
) ON [PRIMARY]
GO


USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[sets]    Script Date: 27/03/2025 14:48:34 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[sets](
	[setID] [int] IDENTITY(1,1) NOT NULL,
	[ExcerciseID] [int] NOT NULL,
	[WorkoutID] [int] NOT NULL,
	[SetDescription] [nchar](50) NULL,
	[SetNotes] [ntext] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


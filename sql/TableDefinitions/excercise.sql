USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[Excercise]    Script Date: 27/03/2025 14:45:46 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Excercise](
	[ExcerciseID] [int] IDENTITY(1,1) NOT NULL,
	[SessionID] [int] NOT NULL,
	[ExcerciseName] [nchar](50) NOT NULL
) ON [PRIMARY]
GO


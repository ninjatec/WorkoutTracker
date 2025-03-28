USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[Users]    Script Date: 27/03/2025 14:31:40 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Users](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[usersname] [char](55) NOT NULL
) ON [PRIMARY]
GO


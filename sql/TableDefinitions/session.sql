USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[Session]    Script Date: 27/03/2025 14:43:25 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Session](
	[SessionID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[SessionDescription] [nchar](120) NOT NULL,
	[Date] [date] NULL,
	[Time] [time](7) NULL
) ON [PRIMARY]
GO


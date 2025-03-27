USE [WorkoutTracker]
GO

/****** Object:  Table [dbo].[types]    Script Date: 27/03/2025 14:55:18 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[types](
	[typeID] [int] IDENTITY(1,1) NOT NULL,
	[Description] [nchar](20) NOT NULL
) ON [PRIMARY]
GO

---


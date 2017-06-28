
USE [MData]
GO
/****** Object:  Table [dbo].[SampleDataModel]    Script Date: 6/28/2017 3:07:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SampleDataModel](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TextData1] [nvarchar](50) NULL,
	[IntData2] [int] NULL,
	[CustomEnum] [smallint] NULL,
	[LastUpdateDate] [datetime] NULL
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[SampleDataModel] ADD  CONSTRAINT [DF_SampleDataTable_LastUpdateDate]  DEFAULT (getdate()) FOR [LastUpdateDate]
GO
/****** Object:  StoredProcedure [dbo].[spGetSampleDataModel]    Script Date: 6/28/2017 3:07:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [dbo].[spGetSampleDataModel]
	@Id [int]  =NULL,
	@TextData1 [nvarchar](50)= NULL,
	@IntData2 [int] =NULL,
	@CustomEnum [smallint] = NULL

AS

BEGIN
	

	SELECT *
	FROM
		SampleDataModel
	WHERE
			Id =IIF (@Id is null, Id  ,@Id) AND
			TextData1 =IIF (@TextData1 is null, TextData1  ,@TextData1) AND
			IntData2 =IIF (@IntData2 is null, IntData2  ,@IntData2) AND
			CustomEnum =IIF (@CustomEnum is null, CustomEnum  ,@CustomEnum) 
			

END

	


GO
/****** Object:  StoredProcedure [dbo].[spUpsertSampleDataModel]    Script Date: 6/28/2017 3:07:11 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [dbo].[spUpsertSampleDataModel]
	@Id [int]  =NULL OUTPUT,
	@TextData1 [nvarchar](50)= NULL,
	@IntData2 [int] =NULL,
	@CustomEnum [smallint] = NULL
AS
BEGIN
	declare @Success bit = 0

	UPDATE
		SampleDataModel
	SET
			TextData1 =IIF (@TextData1 is null, TextData1  ,@TextData1),
			IntData2 =IIF (@IntData2 is null, IntData2  ,@IntData2),
			CustomEnum  =IIF (@CustomEnum is null, CustomEnum  ,@CustomEnum),
			LastUpdateDate = GetDate()
	WHERE
		Id = @Id

	if (@@ROWCOUNT = 1)
		BEGIN
			set @Success = 1
		END

	ELSE 
		BEGIN
		
			INSERT INTO SampleDataModel  (
				TextData1,
				IntData2,
				CustomEnum,
				LastUpdateDate

			)
			values (
				@TextData1,
				@IntData2,
				@CustomEnum,
				GetDate()
			)
			
			if (@@ROWCOUNT = 1)  
			BEGIN
				set @Success = 1
				set @Id = @@IDENTITY
			END


		END

	
	RETURN @Success
END

GO

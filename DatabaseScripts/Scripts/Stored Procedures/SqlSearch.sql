if exists (select * from sysobjects where OBJECTPROPERTY(id, 'IsProcedure') = 1 and id = object_id('tj.SqlSearch'))
drop procedure tj.SqlSearch
GO

CREATE
/*

Name: SqlSearch

Purpose: Do a name search directly in SQL

Note: 


*/
PROCEDURE [tj].[SqlSearch]
    @Election uniqueidentifier
  , @Term1 nvarchar(25)
  , @Term2 nvarchar(25)
  , @Sound1 nvarchar(25)
  , @Sound2 nvarchar(25)
  , @MaxToReturn int
  , @MoreExactMatchesFound bit out
  , @ShowDebugInfo int = 0
AS
  set nocount on

  set @Term1 = '^' + @Term1
  set @Term2 = '^' + @Term2

  set @Sound1 = '^' + @Sound1
  set @Sound2 = '^' + @Sound2

  declare @mode int
  declare @search1 nvarchar(30)
  declare @search2 nvarchar(30)

  declare @hits table (
     RowId int,
	 CharMatch1 int,
	 CharMatch2 int,
	 Score int,
	 Mode int
  )

  set @mode = 0

  while @mode < 2
    begin
	    set @search1 = case when @mode = 0 then @Term1 else @Sound1 end
	    set @search2 = case when @mode = 0 then @Term2 else @Sound2 end

		--TODO: need to search for cases where term1 is AFTER term2
		
		insert into @hits
		select 0 + _RowId [RowId]
			, CHARINDEX(@Search1, '^' + case when @mode = 0 then p.CombinedInfo else p.CombinedSoundCodes end, 0) [CharMatch1]
			, 0 [CharMatch2]
			, 1 + @mode [Score]
			, @mode
		from tj.Person p
		where p.ElectionGuid = @Election
			and CHARINDEX(@Search1, '^' + case when @mode = 0 then p.CombinedInfo else p.CombinedSoundCodes end, 0) > 0
			and LEN(@Search1) > 1

		update h
			set CharMatch2 = CHARINDEX(@Search2, '^' + case when @mode = 0 then p.CombinedInfo else p.CombinedSoundCodes end, 0)
			, Score = Score + 1 + @mode
		from tj.Person p
			join @hits h on h.RowId = p._RowId
		where p.ElectionGuid = @Election
			and CHARINDEX(@Search2, '^' + case when @mode = 0 then p.CombinedInfo else p.CombinedSoundCodes end, coalesce(h.charmatch1 + 1, 0)) > 0
			and h.Mode = @mode
			and LEN(@Search2) > 1

  
      set @mode = @mode + 1
    end






  insert into @hits
    select RowId
	     , 0
		 , 0
		 , 300 -- direct match
		 , 3
	from @hits
	where Mode = 0


  insert into @hits
    select h.RowId
	     , 0
		 , 0
		 , 301 -- fuzzy match
		 , 3
	from @hits h
	where h.Mode = 1
	  and h.RowId not in (select RowId from @hits where Mode = 3)


  --> did use 'into #results' but that fails with SET FMTONLY ON, so EF4 can't read it
  declare @results table (
     RowId int,
	 CharMatch1 int,
	 CharMatch2 int,
	 Score int,
	 Mode int,
	 _FullName nvarchar(500),
	 AgeGroup varchar(20),
	 IneligibleReasonGuid uniqueidentifier,
	 SortOrder int,
	 Votes int
  )


  insert into @results
  select h.*
       , p._FullName
	   , p.AgeGroup
	   , p.IneligibleReasonGuid
	   , ROW_NUMBER() over (order by Score, _FullName) [SortOrder]

	   --TODO: for normal elections, may need to change this
	   , (select SUM(v.SingleNameElectionCount) from tj.vVoteInfo v where v.PersonGuid = p.PersonGuid) [Votes]

   from @hits h
     join tj.Person p on p._RowId = h.RowId
   where 3 = case when @ShowDebugInfo = 1 then 3 else h.Mode end

  set @MoreExactMatchesFound = case when (select COUNT(*) from @results where Score = 300) > @MaxToReturn then 1 else 0 end 


   -- final
   if coalesce(@ShowDebugInfo,0) = 0
     begin
		select RowId [PersonId]
			 , _FullName [FullName]
			 , case when AgeGroup = 'A' and IneligibleReasonGuid is null then 1 else 0 end [Eligible]
			 , case when Score = 301 then 1 
					else 0 end SoundMatch
			 , case when ROW_NUMBER() over (order by Votes desc) = 1 
						 and Score = 300
						 and Votes > 0 
						 then 1 else 0 end [BestMatch]
		from @results
		where SortOrder <= @MaxToReturn
		order by Score, _FullName
	 end
  else
     begin
		select *
			, ROW_NUMBER() over (order by Votes desc)
		from @results
		order by _FullName, Score
	 end

GO

GRANT  EXECUTE  ON [tj].[SqlSearch]  TO [TallyJSite]
GO

-- Testing code
-- / *
declare @more bit
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'g', null, 'kn', '', 15, @more out
select @more [MoreFound], 1
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'gle', null, 'kn', '', 15, @more out
select @more [MoreFound], 2
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'gle', 'li', 'kn', 'l', 15, @more out
select @more [MoreFound], 3
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', '', '', 'kn', 'ltl', 15, @more out
select @more [MoreFound], 4
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'glen', 'little', 'kn', 'ltl', 15, @more out
select @more [MoreFound], 5

SET FMTONLY ON;
exec tj.SqlSearch null, null, null, null, null, null, @more out
SET FMTONLY OFF;


-- * /

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

  declare @sep nvarchar(1)
  set @sep = '^'

  declare @Raw1 nvarchar(25)
  declare @Raw2 nvarchar(25)

  set @Raw1 = coalesce(@Term1,'')
  set @Raw2 = coalesce(@Term2,'')

  set @Term1 = @sep + @Raw1
  set @Term2 = @sep + @Raw2

  set @Sound1 = @sep + coalesce(@Sound1, '') + @sep
  set @Sound2 = @sep + coalesce(@Sound2, '') + @sep



  declare @search1 nvarchar(30)
  declare @search2 nvarchar(30)
  declare @temp nvarchar(30)
  declare @passGroup int

  declare @hits table (
     RowId int,
	 PassNum int,
	 PassGroup int,
	 BothMatched bit,
	 Source nvarchar(2000),
	 FirstMatch int,
	 EndFirstMatch int,
	 Search1 nvarchar(30),
	 Search2 nvarchar(30)
  )

  /* pass numbers

  1 = as typed, starting words
  2 = backwards, starting words
  3 = as typed, sounds like
  4 = backwards, sounds like
  5 = as typed, anywhere
  6 = backwards, anywhere

  */


  if LEN(@term1) > 0
  begin

	  declare @passNum int
	  set @passNum = 1

	  while @passNum <= 6
		begin
		    set @passGroup = (@passNum + 1) / 2
			set @search1 = case @passGroup when 1 then @Term1 when 2 then @Sound1 else @Raw1 end
			set @search2 = case @passGroup when 1 then @Term2 when 2 then @Sound2 else @Raw2 end

			if @passNum % 2 = 1 
			begin
	   		  -- swap every other pass
			  set @temp = @search1
			  set @search1 = @search2
			  set @search2 = @temp
			end

			if @ShowDebugInfo > 4 print cast(@passNum as varchar) + ':' + cast(@passNum % 3 as varchar) + '  --- ' + @Search1 + ' -- ' + @Search2
		

			if LEN(replace(@search1,'^','')) > 0
			begin
			
			   if @ShowDebugInfo > 4 print 'searching'
			 
			   ;with sourceList as (
					select _RowId [RowId]
						, case @passGroup when 1 then @sep + p.CombinedInfo 
										  when 2 then @sep + p.CombinedSoundCodes + @sep
										  else p.CombinedInfo end [Search]
					from tj.Person p
					where p.ElectionGuid = @Election
				)
				insert into @hits
				select m.RowId [RowId]
					, @passNum
					, @passGroup
					, 0
					, left(m.Search, 2000)
					, CHARINDEX(@Search1, m.Search collate Latin1_General_CI_AI, 0) [FirstMatch]
					, 0
					, @search1 -- for debugging
					, @search2
				from sourceList m
				where CHARINDEX(@Search1, Search collate Latin1_General_CI_AI, 0) > 0


				if LEN(replace(@search2,'^','')) > 0
				begin
				
					update @hits
					    set EndFirstMatch = CHARINDEX(@sep, Source collate Latin1_General_CI_AI, FirstMatch)
					where PassNum = @passNum
				
					update @hits
						set BothMatched = 1
					where CHARINDEX(@Search2, Source collate Latin1_General_CI_AI, EndFirstMatch) > 0
						and PassNum = @passNum
				
				end
			end
			
			set @passNum = @passNum + 1

		end
	end

  --can't leave @ShowDebugInfo, or EF4 will read this structure
  --if @ShowDebugInfo > 3 select * from @hits order by 1

  
  if len(coalesce(@Raw2,'')) > 0
  begin
    delete from @hits where BothMatched = 0
  end




  --> did use 'into #results' but that fails with SET FMTONLY ON, so EF4 can't read it
  declare @results table (
     RowId int,
	 FirstMatch int,
	 PassNum int,
	 PassGroup int,
	 _FullName nvarchar(500),
	 AgeGroup varchar(20),
	 IneligibleReasonGuid uniqueidentifier,
	 SortOrder int,
	 Votes int
  )

  ;with byScore as (
     select RowId, PassNum, ROW_NUMBER() over (partition by RowId order by PassNum) RowNum
	 from @hits
  )
  insert into @results
  select h.RowId
       , h.FirstMatch
	   , h.PassNum
	   , h.PassGroup
       , p._FullName
	   , p.AgeGroup
	   , p.IneligibleReasonGuid
	   , ROW_NUMBER() over (order by h.PassNum, _FullName) [SortOrder]

	   , coalesce((select SUM(case when v.IsSingleNameElection = 1 then 1 else v.SingleNameElectionCount end) from tj.vVoteInfo v where v.PersonGuid = p.PersonGuid),0) [Votes]

   from @hits h
     join tj.Person p on p._RowId = h.RowId
	 join byScore s on s.RowId = h.RowId and s.PassNum = h.PassNum
   where s.RowNum = 1

  set @MoreExactMatchesFound = case when (select COUNT(*) from @results where PassNum = 300) > @MaxToReturn then 1 else 0 end 


	select RowId [PersonId]
			, _FullName [FullName]
            , res.IneligibleReasonGuid [Ineligible]
			, res.PassGroup [MatchType]
			, case when ROW_NUMBER() over (order by Votes desc) = 1 
						then 1 else 0 end [BestMatch]
	from @results res
		-- left join tj.Reasons r1 on r1.ReasonGuid = res.IneligibleReasonGuid
	where res.SortOrder <= @MaxToReturn
	order by PassNum, _FullName

GO

GRANT  EXECUTE  ON [tj].[SqlSearch]  TO [TallyJSite]
GO

-- Testing code
-- / *
declare @more bit
exec tj.SqlSearch 'BBA4B2AA-C5A6-4A2F-8B5E-BCC7CEF1C029', 'glen', '', '', '', 15, @more out, 55
select @more [MoreFound], 1
/*exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'gle', null, 'kn', '', 15, @more out
select @more [MoreFound], 2
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'gle', 'li', 'kn', 'l', 15, @more out
select @more [MoreFound], 3
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', '', '', 'kn', 'ltl', 15, @more out
select @more [MoreFound], 4
exec tj.SqlSearch '3936024A-7709-4FAA-9D24-24F7FF933AEE', 'glen', 'little', 'kn', 'ltl', 15, @more out
select @more [MoreFound], 5
exec tj.SqlSearch 'B8153DD7-F6CF-4080-9D7B-30E77033C787', 'mov', null, '', '', 15, @more out

declare @more bit
SET FMTONLY ON;
exec tj.SqlSearch null, null, null, null, null, null, @more out
SET FMTONLY OFF;

select * from tj.Election
-- select * from tj.Person where electionguid = '3936024A-7709-4FAA-9D24-24F7FF933AEE'
-- */

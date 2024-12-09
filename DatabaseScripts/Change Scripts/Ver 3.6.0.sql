-- set the PeopleElectionGuid to the ElectionGuid for all elections that don't have a PeopleElectionGuid

select * 
-- update e set PeopleElectionGuid = ElectionGuid
from tj.Election e
where PeopleElectionGuid is null

-- then change PeopleElectionGuid to be NOT NULL


-- move data from Person to Voter
insert into tj.Voter (
       [ElectionGuid]
      ,[PersonGuid]
      ,[CanVote]
      ,[CanReceiveVotes]
      ,[IneligibleReasonGuid]
      ,[RegistrationTime]
      ,[VotingLocationGuid]
      ,[VotingMethod]
      ,[EnvNum]
      ,[Teller1]
      ,[Teller2]
      ,[HasOnlineBallot]
      ,[KioskCode]
      ,[Flags]
      ,[RegLog]
      )
SELECT   [ElectionGuid]
      ,[PersonGuid]
      ,[CanVote]
      ,[CanReceiveVotes]
      ,[IneligibleReasonGuid]
      ,[RegistrationTime]
      ,[VotingLocationGuid]
      ,[VotingMethod]
      ,[EnvNum]
      ,[Teller1]
      ,[Teller2]
      ,[HasOnlineBallot]
      ,[KioskCode]
      ,[Flags]
      ,SUBSTRING([CombinedSoundCodes], 9, 9999)  -- ~RegLog=2:37 PM; In Person; G
  FROM [tj].[Person]
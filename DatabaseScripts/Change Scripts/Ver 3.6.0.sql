-- set the PeopleElectionGuid to the ElectionGuid for all elections that don't have a PeopleElectionGuid

select * 
-- update e set PeopleElectionGuid = ElectionGuid
from tj.Election e
where PeopleElectionGuid is null

-- then change PeopleElectionGuid to be NOT NULL

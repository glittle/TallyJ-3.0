namespace TallyJ.Code.Session
{
	public static class SessionKey
	{
	  public const string CurrentBallotId = "CurrentBallotId";

	  public const string CurrentElection = "CurrentElection";
	  public const string CurrentComputer = "CurrentComputer";
	  public const string CurrentLocation = "CurrentLocation";
	  public const string CurrentElectionGuid = "ElectionGuid";
	  public const string CurrentTeller = "CurrentTeller";

	  public const string IsGuestTeller = "IsGuestTeller";
	  public const string IsKnownTeller = "IsKnownTeller";

	  public const string CurrentUserGuid = "CurrentUserGuid";
	  public const string LastVersionNum = "LastVersionNum";
	  public const string UserGuidRetrieved = "UserGuidRetrieved";
	  public const string WantedAction = "WantedAction";
    public const string TimeOffsetKnown = "ServerTimeKnown";
    public const string TimeOffset = "ClientServerTimeOffset";

	}
}
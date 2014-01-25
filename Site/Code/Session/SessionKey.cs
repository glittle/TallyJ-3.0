namespace TallyJ.Code.Session
{
	public static class SessionKey
	{
	  public const string CurrentBallotId = "CurrentBallotId";

	  public const string CurrentComputer = "CurrentComputer";
	  public const string CurrentLocationGuid = "LocationGuid";
    public const string CurrentBallotFilter = "BallotFilter";
	  public const string CurrentElectionGuid = "ElectionGuid";
	  public const string CurrentTeller = "CurrentTeller";

	  public const string IsGuestTeller = "IsGuestTeller";
	  public const string IsKnownTeller = "IsKnownTeller";

	  public const string CurrentUserGuid = "CurrentUserGuid";
	  public const string UserGuidRetrieved = "UserGuidRetrieved";
    public const string TimeOffsetKnown = "ServerTimeKnown";
    public const string TimeOffset = "ClientServerTimeOffset";

	}
}
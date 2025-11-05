using System;

[Serializable]
public class CompatibilityVotePayload
{
    public string matchId;
    public string voterUserId;
    public string targetUserId;
    public bool wouldPlayAgain;
    public long unixTime;
}

using System;
[Serializable]
public struct EndorsementPayload
{
    public string matchId;
    public string giverUserId;
    public string receiverUserId;
    public EndorsementType type;
    public long unixTime;
}
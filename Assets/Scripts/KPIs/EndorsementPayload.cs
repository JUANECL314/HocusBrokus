[System.Serializable]
public class EndorsementPayload
{
    public string matchId;
    public string giverUserId;
    public string receiverUserId;

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public EndorsementType type;

    public long unixTime;
}

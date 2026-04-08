namespace Redis;

public enum Status {
    Success,
    Fail,
    OutLimit
} 

public class Result(Status pStatus, string pContext) {
    public Status Status { get; private set; } = pStatus;
    public string Context{ get; private set; } = pContext;
}
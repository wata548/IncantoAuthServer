using System.Text.Json;
namespace Redis;

public enum Status {
    Success,
    Fail,
} 

public class Result(Status pStatus, string pContext) {
    public Status Status { get; private set; } = pStatus;
    public string Context{ get; private set; } = pContext;
}

public class Result<T>(Status pStatus, T pContext) : Result(pStatus, JsonSerializer.Serialize(pContext)) {}
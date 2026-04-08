using StackExchange.Redis;
namespace Redis;

public class RateLimitService {

    private IDatabase _connect;
    
    public RateLimitService(IDatabase pConnect) =>
        _connect = pConnect;

    public async Task Remove(string pKey) {
        const string script = "redis.call('DEL', KEYS[1])";
        await _connect.ScriptEvaluateAsync(script, [pKey], []);
    }

    public async Task<RateLimit> CheckAndIncrement(string pKey, int pLimit, TimeSpan pTime) {

        const string script = @"
local current = redis.call('INCR', KEYS[1]);
if current == 1 then
    redis.call('EXPIRE', KEYS[1], ARGV[1]);
end
local ttl = redis.call('TTL', KEYS[1]);
return {current, ttl};";

        var result = await _connect.ScriptEvaluateAsync(script, [pKey], [(int)pTime.TotalSeconds]);
        var cnt = (int)result[0];
        var remain = (int)result[1];
        return new(pKey, pLimit, cnt, remain);
    }

    public async Task Add(string pKey, string pValue, TimeSpan pTime) {
        const string script = @"redis.call('SETEX', KEYS[1], ARGV[2], ARGV[1]);";
        var result = await _connect.ScriptEvaluateAsync(script, [pKey], [pValue, (int)pTime.TotalSeconds]);
        var cnt = (int)result[0];
        var remain = (int)result[1];
    }

    public async Task<RedisData> Get(string pKey) {
        const string script = @"
local value = redis.call('GET', KEYS[1]);
if not value then 
    return {-2, ''};
local ttl = redis.call('TTL', KEYS[1]);
return {ttl, value};";
        var result = await _connect.ScriptEvaluateAsync(script, [pKey], []);
        var ttl = (int)result[0];
        if (ttl == -2)
            return new("", "", -2);
        var value = result[1]!;
        return new(pKey, value, ttl);
    }
}

public class RedisData(string pKey, object pValue, int pRemain) {
    public readonly string Key = pKey;
    public readonly object Value = pValue;
    public readonly int TTL = pRemain;
}

public class RateLimit(string pKey, int pLimit, int pCnt, int pRemain) {
    public readonly string Key = pKey;
    public readonly int Limit = pLimit;
    public readonly int Cnt = pCnt;
    public readonly int TTL = pRemain;
    public readonly bool Allow = pLimit >= pCnt;
}
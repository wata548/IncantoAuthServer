using Extension;

namespace Match;
public class MatchPlayerResult: ConvertBytes {
        public readonly int Idx;
        public readonly int AliveTime;
        public MatchPlayerResult(int pIdx, int pAliveTime) {
            Idx = pIdx;
            AliveTime = pAliveTime;
        }
        public MatchPlayerResult(byte[] pByte, ref int pStart) {
            Idx = GetInt(pByte, ref pStart);
            AliveTime = GetInt(pByte, ref pStart);
        }

        public override IEnumerable<byte> GetBytes() {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(Idx));
            result.AddRange(BitConverter.GetBytes(AliveTime));
            return result;
        }
    }

public class MatchResult: ConvertBytes {
    public const int Length = MatchPlayers.MatchPerPlayer * 8 + 16;
    public readonly Guid MatchUUID;
    public readonly IReadOnlyList<MatchPlayerResult> Rank;

    public IEnumerable<int> RankedPlayer => Rank.Select(d => d.Idx);

    public MatchResult(byte[] pBytes, ref int pStart) {
        MatchUUID = new(pBytes[pStart..(pStart + 16)]);
        pStart += 16;
        var result = new MatchPlayerResult[MatchPlayers.MatchPerPlayer];
        for (int i = 0; i < result.Length; i++) {
            result[i] = new(pBytes, ref pStart);
        }

        Rank = result;
    }
    
    public override IEnumerable<byte> GetBytes() {
        var result = new List<byte>();
        result.AddRange(MatchUUID.ToByteArray());
        result.AddRange(Rank.SelectMany(m => m.GetBytes()));
        return result;
    }
}
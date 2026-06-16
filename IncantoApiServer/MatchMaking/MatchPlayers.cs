using Extension;

public class MatchPlayers: ConvertBytes {

	//==================================================||Properties	
	
	public const int MatchPerPlayer = 4;

	//==================================================||Methods	
	public MatchPlayers(byte[] pBytes, ref int pStart) {
		var result = new int[MatchPerPlayer];
		for (int i = 0; i < MatchPerPlayer; i++)
			result[i] = GetInt(pBytes, ref pStart);
		Players = result;
		UUID = new Guid(pBytes[pStart..(pStart + 16)]);
	}
	
	public MatchPlayers(List<int> pPlayers, int pStartIdx) {
		Players = 
			pPlayers.Count - pStartIdx >= MatchPerPlayer 
				? pPlayers.Slice(pStartIdx, MatchPerPlayer)
				: throw new IndexOutOfRangeException(
					$"try to access {pStartIdx} ~ {pStartIdx + MatchPerPlayer} (size: {pPlayers.Count})"
				);
		UUID = Guid.NewGuid();
	}
	
	public IReadOnlyCollection<int> Players { get; private set; }
	public readonly Guid UUID;

	//==================================================||Methods	
	public override string ToString() {
		return string.Join(", ", Players);
	}

	public override IEnumerable<byte> GetBytes() {
		var result = new List<byte>();
		foreach (var player in Players) {
			result.AddRange(BitConverter.GetBytes(player));
		}
		result.AddRange(UUID.ToByteArray());

		return result;
	}
}
namespace Extension;

public abstract class ConvertBytes {
	//==================================================Constructors	
	protected ConvertBytes(byte[] pBytes, ref int pStart){}
	protected ConvertBytes(){}
		
	//==================================================Methods	
	public abstract IEnumerable<byte> GetBytes();

	protected int GetInt(byte[] pBytes, ref int pStart) {
		var temp = BitConverter.ToInt32(pBytes, pStart);
		pStart += 4;
		return temp;
	}
	protected float GetSingle(byte[] pBytes, ref int pStart) {
		var temp = BitConverter.ToSingle(pBytes, pStart);
		pStart += 4;
		return temp;
	}
	public string GetString(byte[] pBytes, ref int pStart) {
		var len = GetInt(pBytes, ref pStart);
		var temp = BitConverter.ToString(pBytes, pStart);
		pStart += len;
		return temp;
	}
	protected IEnumerable<byte> GetStringBytes(string pS) {
		var result = new List<byte>();
		result.AddRange(BitConverter.GetBytes(pS.Length));
		result.AddRange(pS.Select(c => (byte)c));
		return result;
	}
	protected bool GetBoolean(byte[] pBytes, ref int pStart) {
		var temp = BitConverter.ToBoolean(pBytes, pStart);
		pStart++;
		return temp;
	}
}

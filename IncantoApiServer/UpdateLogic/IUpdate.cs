namespace IncantoApiServer.UpdateLogic;

public interface IUpdate {
	public Task Update();
}

public abstract class UpdateModule:IUpdate {
	public abstract Task Update();

	protected UpdateModule(UpdateManager pManager) {
		pManager.Add(this);
	}
}
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace IncantoApiServer.UpdateLogic;

public class UpdateManager {
	private readonly ConcurrentBag<IUpdate> _updates = new();
	public void Add(IUpdate pUpdateBase) => _updates.Add(pUpdateBase);
	public IEnumerable<IUpdate> Get() => _updates;
}

public class UpdateLoop(UpdateManager pManager): BackgroundService {
	public const int UpdateInterval = 1000;
	private readonly UpdateManager _manager = pManager;
	
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			foreach (var updateModule in _manager.Get()) {
				await updateModule.Update();
			}

			await Task.Delay(UpdateInterval, stoppingToken);
		}
	}
}
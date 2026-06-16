using System.Collections.Concurrent;
using System.Diagnostics;
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
	
	protected override async Task ExecuteAsync(CancellationToken pStoppingToken) {
		var stopWatch = new Stopwatch();
		while (!pStoppingToken.IsCancellationRequested) {
			stopWatch.Restart();
			await Task.WhenAll(
				_manager.Get()
					.Select(module => module.Update())
			);
			stopWatch.Stop();

			var term = UpdateInterval - (int)stopWatch.ElapsedMilliseconds;
			term = int.Max(0, term);
			await Task.Delay(term, pStoppingToken);
		}
	}
}
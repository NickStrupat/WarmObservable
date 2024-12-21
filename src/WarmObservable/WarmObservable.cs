using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;

namespace NickStrupat;

public static class WarmObservable
{
	public static IObservable<T> From<T>(
		IObservable<T> cold,
		IObservable<T> hot,
		IEqualityComparer<T> ec,
		TimeSpan hotLatencyDelay = default
	)
	{
        ArgumentNullException.ThrowIfNull(cold);
        ArgumentNullException.ThrowIfNull(hot);
        ArgumentNullException.ThrowIfNull(ec);
        ArgumentOutOfRangeException.ThrowIfNegative(hotLatencyDelay.Ticks);
        return new DistinctUntilColdObservableCompletesObservable<T>(cold, hot, ec, hotLatencyDelay);
	}
	
	private sealed class DistinctUntilColdObservableCompletesObservable<T>(
		IObservable<T> cold,
		IObservable<T> hot,
		IEqualityComparer<T> ec,
		TimeSpan hotLatencyDelay
	) : IObservable<T>
	{
		public IDisposable Subscribe(IObserver<T> observer)
		{
			ArgumentNullException.ThrowIfNull(observer);
			var distinctObserver = new DistinctObserver(observer, ec);
			return hot.Merge(cold.Finally(DelayColdCompleted)).Subscribe(distinctObserver);
			void DelayColdCompleted() => _ = distinctObserver.OnColdCompleted(hotLatencyDelay);
		}
	
		private sealed class DistinctObserver(IObserver<T> o, IEqualityComparer<T> ec) : IObserver<T>
		{
			private ConcurrentHashSet<T>? set = new(ec);

			public async Task OnColdCompleted(TimeSpan delay)
			{
				await Task.Delay(delay);
				Interlocked.Exchange(ref set, null);
			}

			public void OnCompleted() => o.OnCompleted();
			public void OnError(Exception error) => o.OnError(error);

			public void OnNext(T value)
			{
				if (set is null || Interlocked.CompareExchange(ref set, null, null) is not { } x || x.Add(value))
					o.OnNext(value);
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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
			var o = new DistinctObserver(observer, ec);
			return hot.Merge(cold.Finally(() => o.OnColdCompleted(hotLatencyDelay))).Subscribe(o);
		}
	
		private sealed class DistinctObserver(IObserver<T> o, IEqualityComparer<T> ec) : IObserver<T>
		{
			private HashSet<T>? set = new(ec);

			public async void OnColdCompleted(TimeSpan delay)
			{
				ArgumentOutOfRangeException.ThrowIfNegative(delay.Ticks);
				await Task.Delay(delay);
				Interlocked.Exchange(ref set, null);
			}

			public void OnCompleted() => o.OnCompleted();
			public void OnError(Exception error) => o.OnError(error);

			public void OnNext(T value)
			{
				if (set is not null && Interlocked.CompareExchange(ref set, null, null) is {} x)
				{
					if (x.Add(value))
					{
						o.OnNext(value);
					}
				}
				else
				{
					o.OnNext(value);
				}
			}
		}
	}
}
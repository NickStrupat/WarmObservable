using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace UnitTests;

public sealed class ObservableAddOnlyCollection<T> : IObservable<T>
{
	private readonly List<T> items = new();
	private event Action<T> added = _ => {};

	public IObservable<T> Added => Observable.FromEvent<T>(h => added += h, h => added -= h);
	public void Add(T item) { lock (items) items.Add(item); added.Invoke(item); }
	public IDisposable Subscribe(IObserver<T> observer) => ToList().ToObservable().Subscribe(observer);
	private List<T> ToList() { lock (items) return items.ToList(); }
}
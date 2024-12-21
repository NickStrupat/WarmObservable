# WarmObservable
A library for combining a cold observable and a hot observable of the same source.

### Main use case
You have a database that you can:
- query for `Thing` rows where `Thing.Color` is red
- subscribe to database notifications for new `Thing` rows where `Thing.Color` is red

WarmObservable allows you to combine these two sources of data into a single observable. The cold observable is used to populate the initial state of the observable, and the hot observable is used to update the observable with new data.

### Subtle but compelling feature:
WarmObservable instances account for the race condition between the cold and hot observables.

Let's say your cold observable emits existing `Thing` rows A, B, and C. Then your hot observable emits the new `Thing` row D. If you were to simply use `cold.Merge(hot)`, any `Thing` rows inserted between the time the cold observable emits and the hot observable emits would be lost. Conversely, if you were to use `hot.Merge(cold)`, any `Thing` rows inserted between the time the hot observable emits and the cold observable emits would be duplicated.

WarmObservables account for this by merging the hot and cold observables, but only emitting distinct `Thing` rows. You may be thinking that behavior could easily be implemented with `hot.Merge(cold).Distinct()`. You would be right! However, `Distinct()` works by keeping a `HashSet` of all the items it has seen so far. The `HashSet` contains all the `Thing` rows ever emitted by the observable (minus duplicates) and never releases them. Over time that will result in a large amount of memory being used.

To address that issue, WarmObservable also keeps a `HashSet` internally. However, once the cold observable has completed the `HashSet` is bypassed and released. At that point, WarmObservable no longer checks for duplicates and the GC may collect the HashSet used previous for distinct checking.

You can also provide a latency delay to account for the time it takes for the hot observable to actually begin emitting events.

## Usage
```csharp
var cold = Observable.FromAsync(async () => await apiClient.GetThingsAsync()).SelectMany(x => x);

var hot = new Subject<Thing>();
await apiClient.SubscribeToNewThingsWithColorAsync(Color.Red, hot.OnNext);

var thingEqualityComparer = EqualityComparer<Thing>.Create(
    (a, b) => a!.Id == b!.Id,
    x => x.Id.GetHashCode()
);
var warm = WarmObservable.From(cold, hot, thingEqualityComparer);
```

[![NuGet Version](https://img.shields.io/nuget/v/WarmObservable)](https://www.nuget.org/packages/WarmObservable)
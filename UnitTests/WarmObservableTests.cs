using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NickStrupat;

namespace UnitTests;

public class WarmObservableTests
{
	[Test, Repeat(10)]
	public void WarmObservable_WhenHotObservablePublishesItemsWithConcurrentContention_NoItemsAreMissedNorDuplicated()
	{
		var observed = new List<Int32>();
		var nums = new ObservableAddOnlyCollection<Int32>();
		var warm = WarmObservable.From(nums, nums.Added, EqualityComparer<Int32>.Default);
		const Int32 max = 1_000_000;
		Parallel.For(0, max, i =>
		{
			nums.Add(i);
			if (i == max / 2)
				warm.Subscribe(x => observed.Add(x));
		});
		observed.Count.Should().Be(max);
		
		// Missing
		Enumerable.Range(0, max).Except(observed).Count().Should().Be(0);
		
		// Duplicates
		observed.GroupBy(x => x).Count(x => x.Take(2).Count() > 1).Should().Be(0);
	}
}
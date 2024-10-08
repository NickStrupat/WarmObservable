using System;
using FluentAssertions;

namespace UnitTests;

public class ObservableAddOnlyCollectionTests
{
	[Test]
	public void Add_PublishesAddedItem()
	{
		var collection = new ObservableAddOnlyCollection<Int32>();
		var added = 0;
		using var sub = collection.Added.Subscribe(_ => added++);
		collection.Add(1);
		added.Should().Be(1);
	}
	
	[Test]
	public void Subscribe_PublishesExistingItems()
	{
		var collection = new ObservableAddOnlyCollection<Int32>();
		collection.Add(1);
		collection.Add(2);
		var added = 0;
		using var sub = collection.Subscribe(_ => added++);
		added.Should().Be(2);
	}
}
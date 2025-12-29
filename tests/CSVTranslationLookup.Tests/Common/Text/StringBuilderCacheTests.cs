// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Tests.Common.Text;

public sealed class StringBuilderCacheTests
{
    [Fact]
    public void Does_Not_Cache_Oversized_Builders()
    {
        StringBuilder largeBuilder = StringBuilderCache.Get(capacity: 10000);
        largeBuilder.Append(new string('x', 20000));
        largeBuilder.Recycle();

        // Get a new builder, should be a fresh small one, not the large cached one
        StringBuilder nextBuilder = StringBuilderCache.Get();

        // If the large one was cached, capacity would be >= 10000
        // Since it shouldn't be cahced, we should get a fresh small one
        Assert.True(nextBuilder.Capacity < 10000, $"Expected small capacity , 10000, got {nextBuilder.Capacity}");
    }

    [Fact]
    public void Caches_Normal_Sized_Buffer()
    {
        StringBuilder first = StringBuilderCache.Get(capacity: 1024);
        first.Append("test content");

        // Get the underling reference (befor recycling clears it)
        object firstRef = first;

        first.Recycle();

        // Get another builder, should be the same instance
        StringBuilder second = StringBuilderCache.Get(capacity: 512);

        // Should be the saeme instance (cached and reused)
        Assert.Same(firstRef, second);

        // Should be cleared
        Assert.Equal(0, second.Length);

        // Shoudl have at least the capacity we requested previously
        Assert.True(second.Capacity >= 1024);

        second.Recycle();
    }

    [Fact]
    public void Clear_Resets_Builder_Property()
    {
        StringBuilder builder = StringBuilderCache.Get(capacity: 100);
        builder.Append("test content that will be cleared");

        int capacityBeforeRecycle = builder.Capacity;
        builder.Recycle();

        StringBuilder reused = StringBuilderCache.Get();

        // Should be cleared
        Assert.Equal(0, reused.Length);

        // Capacity should be preserved (not reset to default)
        Assert.Equal(capacityBeforeRecycle, reused.Capacity);

        reused.Recycle();
    }
}

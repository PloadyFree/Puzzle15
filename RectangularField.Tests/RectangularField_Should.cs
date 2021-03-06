﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using Ninject;
using NUnit.Framework;
using RectangularField.Implementations.Base;
using RectangularField.Interfaces;
using RectangularField.Interfaces.Factories;
using RectangularField.Utils;

namespace RectangularField.Tests
{
    [TestFixture]
    public class RectangularField_Should : TestBase
    {
        #region Factories and fields

        private static readonly IKernel Kernel = new StandardKernel(new RectangularFieldsModule());

        private static IEnumerable<IFieldFactory<T>> GetFactories<T>()
            => Kernel.GetAll<IFieldFactory<T>>();

        private static IEnumerable<IFieldFactory<int>> IntFactories => GetFactories<int>();

        private static IEnumerable<IFieldFactory<string>> StringFactories => GetFactories<string>();

        private static IEnumerable<IFieldFactory<int[]>> ArrayFactories => GetFactories<int[]>();

        private static IEnumerable<IField<int>> IntFields =>
            IntFactories.Select(factory => factory.Create(DefaultFieldSize, DefaultFieldData));

        #endregion

        #region Size tests

        [Test]
        public void WorkWithDifferentSizesCorrectly(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory,
            [Values(-1232, 0, 133)] int width,
            [Values(-13123, 0, 2)] int height)
        {
            var size = new Size(width, height);

            if (size.Height < 0 || size.Width < 0)
            {
                new Action(() => factory.Create(size)).ShouldThrow<Exception>();
            }
            else
            {
                var field = factory.Create(size);

                field.Size.Should().Be(size);
                field.Height.Should().Be(size.Height);
                field.Width.Should().Be(size.Width);
            }
        }

        #endregion

        #region Swap tests

        [Test]
        public void SwapElements(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);

            var original = factory.Create(size,
                1, 2, 3,
                4, 5, 17,
                9, 0, 0);
            var expected = factory.Create(size,
                17, 2, 3,
                4, 5, 1,
                9, 0, 0);

            original = original.Swap(new CellLocation(0, 0), new CellLocation(1, 2));

            original.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void SwapElementAtSamePlace(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);

            var original = factory.Create(size,
                1, 2, 3,
                4, 5, 17,
                9, 0, 0);
            var expected = original.Clone();

            original = original.Swap(new CellLocation(1, 2), new CellLocation(1, 2));

            original.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void FailSwap_WhenElementNotOnField(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            new Action(() => field.Swap(new CellLocation(0, 0), new CellLocation(3, 0)))
                .ShouldThrowExactly<InvalidLocationException>();
        }

        [Test]
        public void SwapElementsOnClonedField(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 2, 3,
                5, 9, 1,
                1, 1, 1);
            var cloned = original.Clone();
            var expected = factory.Create(size,
                1, 2, 3,
                5, 1, 1,
                1, 9, 1);

            cloned = cloned.Swap(new CellLocation(1, 1), new CellLocation(2, 1));

            cloned.Should().BeEquivalentTo(expected);
            cloned.Should().NotBeEquivalentTo(original);
        }

        #endregion

        #region Clone tests

        [Test]
        public void CloneCorrectly(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var cloned = field.Clone();

            cloned.Should().NotBeSameAs(field);
            cloned.Should().BeEquivalentTo(field);
        }

        [Test]
        public void NotChangeOriginalField_AfterChangingClonedField(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var original = field.ToArray();
            var cloned = field.Clone();

            cloned = cloned.Swap(new CellLocation(0, 0), new CellLocation(0, 2));
            cloned = cloned.Swap(new CellLocation(1, 1), new CellLocation(0, 0));
            cloned = cloned.Swap(new CellLocation(2, 0), new CellLocation(2, 2));
            cloned = cloned.Swap(new CellLocation(1, 2), new CellLocation(0, 2));
            cloned = cloned.Swap(new CellLocation(0, 1), new CellLocation(0, 0));

            original.SequenceEqual(cloned).Should().BeFalse();
        }

        #endregion

        #region Enumerate tests

        [Test]
        public void EnumerateLocationsCorrecly(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var expected = new List<CellLocation>();
            for (var row = 0; row < field.Height; row++)
                for (var column = 0; column < field.Width; column++)
                    expected.Add(new CellLocation(row, column));
            field.EnumerateLocations().Should().BeEquivalentTo(expected);
        }

        [Test]
        public void EnumerateCorrectly(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var expected = new List<CellInfo<int>>();
            for (var row = 0; row < field.Height; row++)
                for (var column = 0; column < field.Width; column++)
                {
                    var location = new CellLocation(row, column);
                    expected.Add(new CellInfo<int>(location, field[location]));
                }
            field.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void EnumerateCorrectly_AfterChanges(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 2, 3,
                5, 9, 1,
                1, 1, 1);
            var expected = factory.Create(size,
                10, 9, 1,
                5, 3, 1,
                1, 1, 11);

            original = original.Swap(new CellLocation(0, 0), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(0, 0));
            original = original.Swap(new CellLocation(2, 0), new CellLocation(2, 2));
            original = original.Swap(new CellLocation(1, 2), new CellLocation(0, 2));
            original = original.SetValue(10, new CellLocation(0, 1));
            original = original.Swap(new CellLocation(0, 1), new CellLocation(0, 0));
            original = original.SetValue(11, new CellLocation(2, 2));

            original.ToList().Should().BeEquivalentTo(expected);
        }

        #endregion

        #region GetLocations tests

        [Test]
        public void ReturnLocations_ForNonNulls(
            [ValueSource(nameof(StringFactories))] IFieldFactory<string> factory)
        {
            var field = factory.Create(new Size(3, 3),
                "aa", "asda", null,
                "rr", null, "asda",
                "asda", "fdfg", "lel");

            field.GetLocations("asda").Should()
                .BeEquivalentTo(new CellLocation(0, 1), new CellLocation(1, 2), new CellLocation(2, 0));
        }

        [Test]
        public void ReturnLocations_ForNulls(
            [ValueSource(nameof(StringFactories))] IFieldFactory<string> factory)
        {
            var field = factory.Create(new Size(3, 3),
                "aa", "asda", null,
                "rr", null, "asda",
                "asda", "fdfg", "lel");

            field.GetLocations(null).Should()
                .BeEquivalentTo(new CellLocation(0, 2), new CellLocation(1, 1));
        }

        [Test]
        public void ReturnLocations_WhenNotFound(
            [ValueSource(nameof(StringFactories))] IFieldFactory<string> factory)
        {
            var field = factory.Create(new Size(3, 3),
                "aa", "asda", null,
                "rr", null, "asda",
                "asda", "fdfg", "lel");

            field.GetLocations("some other string").Should().BeEmpty();
        }

        [Test]
        public void ReturnLocations_AfterChangesByIndex(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 2, 3,
                5, 9, 1,
                1, 1, 1);

            var expectedLocations = new Dictionary<int, CellLocation[]>
            {
                {0, new[] {new CellLocation(2, 1)}},
                {
                    1,
                    new[]
                    {new CellLocation(0, 0), new CellLocation(0, 2), new CellLocation(2, 0), new CellLocation(2, 2)}
                },
                {2, new[] {new CellLocation(0, 1)}},
                {5, new[] {new CellLocation(1, 0)}},
                {9, new[] {new CellLocation(1, 1)}},
                {200, new[] {new CellLocation(1, 2)}}
            };

            original = original.SetValue(1, new CellLocation(0, 2));
            original = original.SetValue(0, new CellLocation(2, 1));
            original = original.SetValue(200, new CellLocation(1, 2));
            original = original.SetValue(1, new CellLocation(2, 0));

            //  Should be
            //  1 2 1
            //  5 9 200
            //  1 0 1

            var realLocations = new Dictionary<int, CellLocation[]>();
            foreach (var value in original.Select(x => x.Value))
                realLocations[value] = original.GetLocations(value).ToArray();

            realLocations.Count.Should().Be(expectedLocations.Count);
            foreach (var numberAndLocations in realLocations)
                // ReSharper disable once CoVariantArrayConversion
                numberAndLocations.Value.Should().BeEquivalentTo(expectedLocations[numberAndLocations.Key]);
        }

        [Test]
        public void ReturnLocations_AfterSwaps(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var original = factory.Create(new Size(3,3),
                1, 2, 3,
                5, 9, 1,
                1, 1, 1);

            var expectedLocations = new Dictionary<int, CellLocation[]>
            {
                {
                    1,
                    new[]
                    {
                        new CellLocation(0, 2),
                        new CellLocation(1, 2),
                        new CellLocation(2, 0), new CellLocation(2, 1), new CellLocation(2, 2)
                    }
                },
                {2, new[] {new CellLocation(0, 1)}},
                {3, new[] {new CellLocation(1, 1)}},
                {5, new[] {new CellLocation(1, 0)}},
                {9, new[] {new CellLocation(0, 0)}}
            };

            original = original.Swap(new CellLocation(0, 0), new CellLocation(1, 1));
            original = original.Swap(new CellLocation(2, 1), new CellLocation(2, 0));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(1, 1));

            //  Should be
            //  9 2 1
            //  5 3 1
            //  1 1 1

            var realLocations = new Dictionary<int, CellLocation[]>();
            foreach (var value in original.Select(x => x.Value))
                realLocations[value] = original.GetLocations(value).ToArray();

            realLocations.Count.Should().Be(expectedLocations.Count);
            foreach (var numberAndLocations in realLocations)
                // ReSharper disable once CoVariantArrayConversion
                numberAndLocations.Value.Should().BeEquivalentTo(expectedLocations[numberAndLocations.Key]);
        }

        [Test]
        public void ReturnLocations_WithoutChangingClonedField(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 5, 3,
                0, 2, 8,
                7, 4, 6);
            var cloned = original.Clone();
            original.Should().BeEquivalentTo(cloned);

            original = original.Swap(new CellLocation(0, 0), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(0, 0));
            original = original.Swap(new CellLocation(2, 0), new CellLocation(2, 2));
            original = original.Swap(new CellLocation(1, 2), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(0, 1), new CellLocation(0, 0));

            var expectedOrder = new[] {5, 2, 8, 0, 3, 1, 6, 4, 7};
            original.EnumerateLocations()
                .OrderBy(x => x)
                .Zip(expectedOrder, (location, number) => new
                {
                    RealLocation = original.GetLocation(number),
                    ExpectedLocation = location
                })
                .Select(x => x.RealLocation.Equals(x.ExpectedLocation))
                .ShouldAllBeEquivalentTo(true);

            original.Should().NotBeEquivalentTo(cloned);
        }

        #endregion

        #region Indexer tests

        [Test]
        public void ReturnCorrectValues(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var values = new[]
            {
                1, 2, 3,
                5, 9, 1,
                1, 1, 1
            };
            var original = factory.Create(size, values.ToArray());

            foreach (var location in original.EnumerateLocations())
                original[location].Should().Be(values[location.Row*size.Width + location.Column]);
        }

        [Test]
        public void ReturnCorrectValuesByIndex_AfterChanges(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 2, 3,
                5, 9, 1,
                1, 1, 1);
            original = original.Swap(new CellLocation(0, 0), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(0, 0));
            original = original.Swap(new CellLocation(2, 0), new CellLocation(2, 2));
            original = original.Swap(new CellLocation(1, 2), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(0, 1), new CellLocation(0, 0));

            var expected = new[]
            {
                2, 9, 1,
                5, 3, 1,
                1, 1, 1
            };

            foreach (var location in original.EnumerateLocations())
                original[location].Should().Be(expected[location.Row*size.Width + location.Column]);
        }

        [Test]
        public void ReturnCorrectValuesByIndex_OnClonedField(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var original = factory.Create(size,
                1, 2, 3,
                5, 9, 1,
                1, 1, 1).Clone();
            original = original.Swap(new CellLocation(0, 0), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(1, 1), new CellLocation(0, 0));
            original = original.Swap(new CellLocation(2, 0), new CellLocation(2, 2));
            original = original.Swap(new CellLocation(1, 2), new CellLocation(0, 2));
            original = original.Swap(new CellLocation(0, 1), new CellLocation(0, 0));

            var expected = new[]
            {
                2, 9, 1,
                5, 3, 1,
                1, 1, 1
            };

            foreach (var location in original.EnumerateLocations())
                original[location].Should().Be(expected[location.Row*size.Width + location.Column]);
        }

        [Test]
        public void FailSetIndexer_WhenFieldIsImmutable(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var cloned = field.Clone();
            var set = new Action(() => field[new CellLocation(0, 0)] = 123);

            if (field.Immutable)
            {
                set.ShouldThrow<Exception>();
            }
            else
            {
                set.ShouldNotThrow();
                field.Should().NotBeEquivalentTo(cloned);
                cloned[new CellLocation(0, 0)] = 123;
            }

            field.Should().BeEquivalentTo(cloned);
        }

        [Test]
        public void FailSetValue_WhenLocationOutOfRange(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            var cloned = field.Clone();
            var size = field.Size;

            var failingLocations = new[]
            {
                new CellLocation(-1, -1),
                new CellLocation(size.Height, 0),
                new CellLocation(0, size.Width),
                new CellLocation(size.Height + 100500, size.Width + 15),
                new CellLocation(-1, 0),
                new CellLocation(0, -1)
            };

            foreach (var location in failingLocations)
            {
                new Action(() => field = field.SetValue(0, location)).ShouldThrowExactly<InvalidLocationException>();
                field.Should().BeEquivalentTo(cloned);
            }
        }

        #endregion

        #region Equals tests

        [Test]
        public void BeEqual_ToSelf(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            // ReSharper disable once EqualExpressionComparison
            field.Equals(field).Should().BeTrue();
        }

        [Test]
        public void BeEqual_ToClonedField(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            field.Equals(field.Clone()).Should().BeTrue();
        }

        [Test]
        public void BeEqual_ToEquivalentField(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var field1 = factory.Create(size,
                1, 2, 3,
                4, 9, 0,
                -1, 6, 9);
            var field2 = factory.Create(size,
                1, 2, 3,
                4, 9, 0,
                -1, 6, 9);

            field1.Equals(field2).Should().BeTrue();
        }

        [Test]
        public void NotBeEqual_ToNotEquivalentField(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var size = new Size(3, 3);
            var field1 = factory.Create(size,
                1, 2, 3,
                4, 9, 0,
                -1, 6, 8);
            var field2 = factory.Create(size,
                1, 2, 3,
                4, 9, 0,
                -1, 6, 9);

            field1.Equals(field2).Should().BeFalse();
        }

        [Test]
        public void NotBeEqual_ToNull(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            field.Equals(null).Should().BeFalse();
        }

        [Test]
        public void BeEqual_WhenEqualArraysUsed(
            [ValueSource(nameof(ArrayFactories))] IFieldFactory<int[]> factory)
        {
            var size = new Size(2, 2);

            var field1 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, null);
            var field2 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, null);

            field1.Equals(field2).Should().BeTrue();
        }

        [Test]
        public void BeNotEqual_WhenNotEqualArraysUsed(
            [ValueSource(nameof(ArrayFactories))] IFieldFactory<int[]> factory)
        {
            var size = new Size(2, 2);

            var field1 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, new int[0]);
            var field2 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, null);

            field1.Equals(field2).Should().BeFalse();
        }

        #endregion

        #region GetHashCode tests

        [Test]
        public void ReturnSameHash_WhenCalledTwice(
            [ValueSource(nameof(IntFields))] IField<int> field)
        {
            field.GetHashCode().Should()
                .Be(field.GetHashCode());
        }

        [Test]
        public void ReturnSameHash_ForEqualFields(
            [ValueSource(nameof(IntFactories))] IFieldFactory<int> factory)
        {
            var field1 = factory.Create(DefaultFieldSize, DefaultFieldData);
            var field2 = factory.Create(DefaultFieldSize, DefaultFieldData);

            var hash1 = field1.GetHashCode();
            var hash2 = field2.GetHashCode();

            hash1.Should().Be(hash2);
        }

        [Test]
        public void ReturnSameHash_ForFieldsWithArray(
            [ValueSource(nameof(ArrayFactories))] IFieldFactory<int[]> factory)
        {
            var size = new Size(2, 2);

            var field1 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, null);
            var field2 = factory.Create(size,
                new[] {1, 2}, new[] {3, 4},
                new[] {4, 4}, null);

            var hash1 = field1.GetHashCode();
            var hash2 = field2.GetHashCode();

            hash1.Should().Be(hash2);
        }

        #endregion
    }
}

﻿using System;
using System.Drawing;
using FluentAssertions;
using NUnit.Framework;
using Puzzle15.Implementations;
using Puzzle15.Interfaces;
using RectangularField.Implementations;
using RectangularField.Utils;

namespace Puzzle15.Tests
{
    [TestFixture]
    public class Game_Should : TestBase
    {
        private IGameFactory gameFactory;

        private static FieldConstructor<T> GetImmutableFieldConstructor<T>()
        {
            return sz => new ImmutableRectangularField<T>(sz);
        }

        private static FieldConstructor<T> GetMutableFieldConstructor<T>()
        {
            return sz => new RectangularField<T>(sz);
        }

        [SetUp]
        public void SetUp()
        {
            gameFactory = new GameFactory(new GameFieldValidator());
        }

        #region Consistency tests

        [Test]
        public void NotChangeOurField_AfterCreating()
        {
            var fieldConstructor = GetMutableFieldConstructor<int>();
            var field = FieldFromConstructor(fieldConstructor, new Size(3, 3),
                1, 2, 3,
                6, 0, 4,
                7, 5, 8);
            var clonedField = field.Clone();
            var game = gameFactory.Create(field);

            field.Should().BeEquivalentTo(clonedField);

            game.Shift(5);

            game.Should().NotBeEquivalentTo(field);
            field.Should().BeEquivalentTo(clonedField);
        }

        #endregion

        #region Shift tests

        [Test]
        public void ShiftCorrectly_WhenValueOnFieldAndConnectedByEdge()
        {
            var fieldConstructor = GetMutableFieldConstructor<int>();
            var size = new Size(3, 3);
            var field = FieldFromConstructor(fieldConstructor, size,
                1, 2, 3,
                6, 0, 4,
                7, 5, 8);
            var game = gameFactory.Create(field);

            game.Shift(5);

            var expectedField = FieldFromConstructor(fieldConstructor, size,
                1, 2, 3,
                6, 5, 4,
                7, 0, 8);
            game.Should().BeEquivalentTo(expectedField);
        }

        [Test]
        public void FailShift_WhenValueNotOnFieldOrNotConnectdByEdge(
            [Values(-1, 10, 100, 8, 1, 2, 0)] int value)
        {
            var fieldConstructor = GetMutableFieldConstructor<int>();
            var size = new Size(3, 3);
            var field = FieldFromConstructor(fieldConstructor, size,
                1, 2, 3,
                7, 5, 8,
                6, 0, 4);
            var game = gameFactory.Create(field);

            new Action(() => game.Shift(value)).ShouldThrow<Exception>();
            game.Should().BeEquivalentTo(field);
        }

        #endregion

        #region Creating new game on shift tests

        [Test]
        public void ReturnSameGameOnShift_WhenFieldIsMutable()
        {
            var fieldConstructor = GetMutableFieldConstructor<int>();
            var field = FieldFromConstructor(fieldConstructor, new Size(3, 3),
                1, 2, 3,
                6, 0, 4,
                7, 5, 8);
            var game = gameFactory.Create(field);

            game.Shift(5).Should().BeSameAs(game);
        }

        [Test]
        public void ReturnNewGameOnShift_WhenFieldIsImmutable()
        {
            var fieldConstructor = GetImmutableFieldConstructor<int>();
            var size = new Size(3, 3);
            var field = FieldFromConstructor(fieldConstructor, size,
                1, 2, 3,
                6, 0, 4,
                7, 5, 8);
            var game = gameFactory.Create(field);

            var newGame = game.Shift(5);
            newGame.Should().NotBeSameAs(game);

            var expectedField = FieldFromConstructor(fieldConstructor, size,
                1, 2, 3,
                6, 5, 4,
                7, 0, 8);
            newGame.Should().BeEquivalentTo(expectedField);
        }

        #endregion
    }
}

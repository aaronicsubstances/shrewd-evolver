using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class LogNavigatorTest
    {
        class LogPositionHolderImpl
        {
            public LogPositionHolderImpl(string positionId)
            {
                PositionId = positionId;
            }

            public string PositionId { get; }
        }

        [Fact]
        public void TestEmptiness()
        {
            var instance = new LogNavigator<LogPositionHolderImpl>(new List<LogPositionHolderImpl>());
            Assert.False(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
        }

        [Fact]
        public void TestNext()
        {
            var testLogs = new List<LogPositionHolderImpl>
            {
                new LogPositionHolderImpl("a"),
                new LogPositionHolderImpl("b"),
                new LogPositionHolderImpl("c"),
                new LogPositionHolderImpl("c")
            };
            var instance = new LogNavigator<LogPositionHolderImpl>(testLogs);

            for (int i = 0; i < testLogs.Count; i++)
            {
                Assert.True(instance.HasNext());
                Assert.Equal(i, instance.NextIndex);
                Assert.Equal(testLogs[i], instance.Next());
            }

            Assert.False(instance.HasNext());
            Assert.Equal(4, instance.NextIndex);
        }

        [Fact]
        public void TestNextWithSearchIds()
        {
            var testLogs = new List<LogPositionHolderImpl>{
                new LogPositionHolderImpl("a"),
                new LogPositionHolderImpl("b"),
                new LogPositionHolderImpl("c"),
                new LogPositionHolderImpl("c")
            };
            var instance = new LogNavigator<LogPositionHolderImpl>(testLogs);

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "d"));

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Equal(testLogs[1], instance.Next(x => new List<string> { "c", "b" }.Contains(x.PositionId)));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Equal(testLogs[2], instance.Next(x => x.PositionId == "c"));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "a"));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
            Assert.Equal(testLogs[3], instance.Next(x => x.PositionId == "c"));

            Assert.False(instance.HasNext());
            Assert.Equal(4, instance.NextIndex);
        }

        [Fact]
        public void TestNextWithSearchAndLimitIds()
        {
            var testLogs = new List<LogPositionHolderImpl>
            {
                new LogPositionHolderImpl("a"),
                new LogPositionHolderImpl("b"),
                new LogPositionHolderImpl("c"),
                new LogPositionHolderImpl("c")
            };
            var instance = new LogNavigator<LogPositionHolderImpl>(testLogs);

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "d", null));

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Equal(testLogs[0], instance.Next(x => x.PositionId == "a", x => x.PositionId == "b"));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "c", x => x.PositionId == "b"));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "b", x => x.PositionId == "b"));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Equal(testLogs[1], instance.Next(x => x.PositionId == "b", x => x.PositionId == "c"));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Null(instance.Next(x => x.PositionId == "c", x => x.PositionId == "c"));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Equal(testLogs[2], instance.Next(x => x.PositionId == "c", null));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
        }
    }
}

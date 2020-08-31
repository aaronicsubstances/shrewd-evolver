using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class LogNavigatorTest
    {
        class LogPositionHolderImpl: ILogPositionHolder
        {
            private readonly string _positionId;
            
            public LogPositionHolderImpl(string positionId)
            {
                _positionId = positionId;
            }

            public string LoadPositionId()
            {
                return _positionId;
            }
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
            Assert.Null(instance.Next(new List<string> { "d" }));

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Equal(testLogs[1], instance.Next(new List<string> { "c", "b" }));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Equal(testLogs[2], instance.Next(new List<string> { "c" }));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
            Assert.Null(instance.Next(new List<string> { "a" }));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
            Assert.Equal(testLogs[3], instance.Next(new List<string> { "c" }));

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
            Assert.Null(instance.Next(new List<string> { "d" }, null));

            Assert.True(instance.HasNext());
            Assert.Equal(0, instance.NextIndex);
            Assert.Equal(testLogs[0], instance.Next(new List<string> { "a" }, new List<string> { "b" }));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Null(instance.Next(new List<string> { "c" }, new List<string> { "b" }));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Null(instance.Next(new List<string> { "b" }, new List<string> { "b" }));

            Assert.True(instance.HasNext());
            Assert.Equal(1, instance.NextIndex);
            Assert.Equal(testLogs[1], instance.Next(new List<string> { "b" }, new List<string> { "c" }));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Null(instance.Next(new List<string> { "c" }, new List<string> { "c" }));

            Assert.True(instance.HasNext());
            Assert.Equal(2, instance.NextIndex);
            Assert.Equal(testLogs[2], instance.Next(new List<string> { "c" }, null));

            Assert.True(instance.HasNext());
            Assert.Equal(3, instance.NextIndex);
        }
    }
}

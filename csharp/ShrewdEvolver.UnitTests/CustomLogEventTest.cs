using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Xunit;
using static AaronicSubstances.ShrewdEvolver.UnitTests.TestUtils;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class CustomLogEventTest
    {
        [Fact]
        public void TestAddProperty()
        {
            var instance = new CustomLogEvent(GetType());
            Assert.Null(instance.Data);

            instance.AddProperty("age", 20);
            Assert.Equal(new Dictionary<string, object>
            {
                { "age", 20 }
            }, instance.Data);

            instance.AddProperty("name", "Kofi");
            Assert.Equal(new Dictionary<string, object>
            {
                { "age", 20 },
                { "name", "Kofi" }
            }, instance.Data);
        }

        [Fact]
        public void TestGenerateMessage()
        {
            var instance = new CustomLogEvent(GetType());
            dynamic properties = new ExpandoObject();
            instance.Data = properties;
            properties.person = new Dictionary<string, string>
            {
                { "name", "Kofi" }
            };
            properties.age = "twenty";

            instance.GenerateMessage((j, s) => $"{j($"person/name")} is " +
                $"{s($"age")} years old.");
            Assert.Equal("\"Kofi\" is twenty years old.", instance.Message);
        }

        [Theory]
        [MemberData(nameof(CreatTestGetTreeDataSliceData))]
        public void TestGetTreeDataSlice(object treeData, string path, object expected)
        {
            var instance = new CustomLogEvent(null)
            {
                Data = treeData
            };
            object actual = instance.FetchDataSlice(path);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreatTestGetTreeDataSliceData()
        {
            return new List<object[]>
            {
                new object[]{ null, "", null },
                new object[]{ "", "", "" },
                new object[]{ 2, "", 2 },
                new object[]{ ToList(2), "f", null },
                new object[]{ ToList(2), "0", 2 },
                new object[]{ ToList(21, ToList()), "10", null },
                new object[]{ ToList(21, ToList()), "0", 21 },
                new object[]{ ToList(21, ToList()), "1", ToList() },
                new object[]{ ToList(21, ToList()), "-1", ToList() },
                new object[]{ ToList(21, ToList()), "-2", null },
                new object[]{ ToDict("a", 1), "", ToDict("a", 1) },
                new object[]{ ToDict("a", 1), "0", null },
                new object[]{ ToDict("a", 1), "a", 1 },
                new object[]{ ToDict("a", 1, "b", 2), "b", 2 },
                new object[]{ ToDict("a", 1, "b", 2), "c", null },
                new object[]{ ToDict("a", 1, "b", ToList("e", true)), "b/0", "e" },
                new object[]{ ToDict("a", 1, "b", ToList("e", true)), "b/1", true },
                new object[]{ ToDict("a", 1, "b", ToDict("e", true)), "b", ToDict("e", true) },
                new object[]{ ToDict("a", 1, "b", ToDict("e", true)), "b/e", true }
            };
        }
    }
}

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class MyLexerTest
    {
        [Fact]
        public void Test()
        {
            string code = File.ReadAllText($@"..\..\..\{GetType().Name}.cs");
            var tokens = MyLexer.Parse(code);
            var writer = LogManager.GetCurrentClassLogger();
            foreach (var t in tokens)
            {
                writer.Info(JsonConvert.SerializeObject(t));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.Json;

namespace RT.Util
{
    [TestFixture]
    public sealed class JsonTests
    {
        [Test]
        public void TestJsonInstantiate()
        {
            Assert.DoesNotThrow(() => new JsonBool(true));
            Assert.DoesNotThrow(() => new JsonNumber(25));
            Assert.DoesNotThrow(() => new JsonString("abc"));
            Assert.DoesNotThrow(() => new JsonList());
            Assert.DoesNotThrow(() => new JsonDict());

            Assert.Throws<ArgumentNullException>(() => new JsonString(null));

            Assert.Throws<ArgumentException>(() => new JsonNumber(double.NaN));
            Assert.Throws<ArgumentException>(() => new JsonNumber(double.PositiveInfinity));
        }

        [Test]
        public void TestJsonEquality()
        {
            // Comparison with null
            Assert.IsFalse(new JsonBool(true).Equals(null));
            Assert.IsFalse(new JsonString("thingy").Equals(null));
            Assert.IsFalse(new JsonNumber(47).Equals(null));
            Assert.IsFalse(new JsonList().Equals(null));
            Assert.IsFalse(new JsonDict().Equals(null));

            // Comparison with an object ouside the hierarchy
            Assert.IsFalse(new JsonBool(true).Equals(new JsonString("abc")));
            Assert.IsFalse(new JsonString("thingy").Equals(StringComparer.Ordinal));
            Assert.IsFalse(new JsonNumber(47).Equals(new StringBuilder()));
            Assert.IsFalse(new JsonList().Equals(new JsonDict()));
            Assert.IsFalse(new JsonDict().Equals(new object()));

            // Normal comparisons: bool
            Assert.IsTrue(new JsonBool(true).Equals(new JsonBool(true)));
            Assert.IsFalse(new JsonBool(true).Equals(new JsonBool(false)));
            Assert.IsTrue(new JsonBool(true) == true);
            Assert.IsTrue(new JsonBool(false) != true);

            // Normal comparisons: string
            Assert.IsTrue(new JsonString("abc").Equals(new JsonString("abc")));
            Assert.IsFalse(new JsonString("thingy").Equals(new JsonString("stuff")));
            Assert.IsTrue(new JsonString("abc") == "abc");
            Assert.IsTrue(new JsonString("thingy") != "stuff");

            // Normal comparisons: number
            Assert.IsTrue(new JsonNumber(47.0).Equals(new JsonNumber(47m)));
            Assert.IsTrue(new JsonNumber(47.0).Equals(new JsonNumber(47)));
            Assert.IsTrue(new JsonNumber(47.0).Equals(new JsonNumber(47L)));
            Assert.IsFalse(new JsonNumber(47.47).Equals(new JsonNumber(47)));
            Assert.IsTrue(new JsonNumber(47) == 47.0);
            Assert.IsTrue(new JsonNumber(47.4) != 47.47m);

            // Normal comparisons: list
            Assert.IsTrue(new JsonList().Equals(new JsonList()));
            Assert.IsTrue(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }.Equals(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }));
            Assert.IsFalse(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }.Equals(new JsonList { 47, null, "blah", new JsonList { -3.1 }, 5m, -0.12 }));
            Assert.IsFalse(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }.Equals(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12, null }));

            // Normal comparisons: dict
            Assert.IsTrue(new JsonDict().Equals(new JsonDict()));
            Assert.IsTrue(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }.Equals(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }));
            Assert.IsTrue(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }.Equals(new JsonDict { { "blah", null }, { "hey", new JsonDict { { "inner", "self" } } }, { "", 47 } }));
            Assert.IsFalse(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }.Equals(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } } }));
            Assert.IsFalse(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }.Equals(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "selfish" } } }, { "blah", null } }));
            Assert.IsFalse(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }.Equals(new JsonDict { { "", 47 }, { "hey!", new JsonDict { { "inner", "self" } } }, { "blah", null } }));

            // Normal comparisons via JsonValue
            assertValueEqual(new JsonBool(true), new JsonBool(true));
            assertValueNotEqual(new JsonBool(true), new JsonBool(false));
            assertValueEqual(new JsonString("abc"), new JsonString("abc"));
            assertValueNotEqual(new JsonString("thingy"), new JsonString("stuff"));
            assertValueEqual(new JsonNumber(47.0), new JsonNumber(47L));
            assertValueNotEqual(new JsonNumber(47.47), new JsonNumber(47));
            assertValueEqual(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }, new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 });
            assertValueNotEqual(new JsonList { 47, null, "blah", new JsonList { -3 }, 5m, -0.12 }, new JsonList { 47, null, "blah", new JsonList { -3.1 }, 5m, -0.12 });
            assertValueEqual(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }, new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } });
            assertValueNotEqual(new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } }, { "blah", null } }, new JsonDict { { "", 47 }, { "hey", new JsonDict { { "inner", "self" } } } });

            // JsonValue comparisons with nulls and objects outside the hierarchy
            assertJsonValueNotEqualComparison(new JsonBool(true));
            assertJsonValueNotEqualComparison(new JsonString("abc"));
            assertJsonValueNotEqualComparison(new JsonNumber(47.5));
            assertJsonValueNotEqualComparison(new JsonList());
            assertJsonValueNotEqualComparison(new JsonDict());
        }

        private void assertValueEqual(JsonValue val1, JsonValue val2)
        {
            Assert.IsTrue(val1.Equals(val2));
        }

        private void assertValueNotEqual(JsonValue val1, JsonValue val2)
        {
            Assert.IsFalse(val1.Equals(val2));
        }

        private void assertJsonValueNotEqualComparison(JsonValue value)
        {
            Assert.IsFalse(value.Equals(null));
            if (!(value is JsonBool)) Assert.IsFalse(value.Equals(new JsonBool(true)));
            if (!(value is JsonString)) Assert.IsFalse(value.Equals(new JsonString("abc")));
            if (!(value is JsonNumber)) Assert.IsFalse(value.Equals(new JsonNumber(47)));
            if (!(value is JsonList)) Assert.IsFalse(value.Equals(new JsonList()));
            if (!(value is JsonDict)) Assert.IsFalse(value.Equals(new JsonDict()));
        }

        [Test]
        public void TestJsonBool()
        {
            assertBoolParseSucc("true", true);
            assertBoolParseSucc("false", false);

            assertBoolParseFail("");
            assertBoolParseFail("1");
            assertBoolParseFail("abc");
        }

        [Test]
        public void TestJsonString()
        {
            assertStringParseSucc("\"\"", "");
            assertStringParseSucc("\"ab cd ef\"", "ab cd ef");
            assertStringParseSucc("\"thingy's \\\"here\\\" now!\"", "thingy's \"here\" now!");
            assertStringParseSucc("\"русский \\u1234\"", "русский \u1234");
            assertStringParseSucc("\"some\\r\\nnewlines\"", "some\r\nnewlines");
            assertStringParseSucc("\"what\\u0027s up?\"", "what's up?");

            assertStringParseFail("");
            assertStringParseFail("123");
            assertStringParseFail("abc");
            assertStringParseFail("'abc'");
            assertStringParseFail("\"what\\'s up?\"");
            assertStringParseFail("\"abc");
            assertStringParseFail("abc\"");
            assertStringParseFail("\"abc\\");
            assertStringParseFail("\"abc\\n");
            assertStringParseFail("\"abc\\u");
            assertStringParseFail("\"abc\\u27");
            assertStringParseFail("\"abc\\\"");
            assertStringParseFail("\"abc\\u\"");
            assertStringParseFail("\"abc\\u27\"");
        }

        [Test]
        public void TestJsonNumber()
        {
            assertNumberParseSucc("1", 1);
            assertNumberParseSucc("100.00", 100);
            assertNumberParseSucc("-1", -1);
            assertNumberParseSucc("0", 0);
            assertNumberParseSucc("2.5E4", 25000);
            assertNumberParseSucc("2.5e4", 25000);
            assertNumberParseSucc("2.5e+4", 25000);
            assertNumberParseSucc("2.5e-4", .00025, .00025m);
            assertNumberParseSucc("5e0", 5);
            assertNumberParseSucc("5e003", 5000);
            assertNumberParseSucc("0.1", null, .1m);
            assertNumberParseSucc("0.12345678909876543210123", null, 0.12345678909876543210123m);
            assertNumberParseSucc("12345678909876543210123", null, 12345678909876543210123m);
            assertNumberParseSucc("123e300", 123e300, null);
            assertNumberParseSucc("123e-300", 123e-300, null);

            assertNumberParseFail("");
            assertNumberParseFail("01");
            assertNumberParseFail("abc");
            assertNumberParseFail("e47");
            assertNumberParseFail("+5");
            assertNumberParseFail("5..3");
            assertNumberParseFail("4.7e4.2");
            assertNumberParseFail(".123"); // json disallows
            assertNumberParseFail("123."); // json disallows
        }

        [Test]
        public void TestJsonList()
        {
            assertListParseSucc("[]", new JsonList { });
            assertListParseSucc("[47]", new JsonList { 47 });
            assertListParseSucc("[47, null, [  98   ,  true   ]    ,\"thingy\"]", new JsonList { 47, null, new JsonList { 98, true }, "thingy" });

            assertListParseFail("");
            assertListParseFail("null");
            assertListParseFail("47");
            assertListParseFail("[47");
            assertListParseFail("47]");
            assertListParseFail("[47 47]");
            assertListParseFail("[47, , 47]");
            assertListParseFail("[47, 47,]");
            assertListParseFail("[ , 47, 47]");
        }

        [Test]
        public void TestJsonDict()
        {
            assertDictParseSucc("{}", new JsonDict { });
            assertDictParseSucc("{ \"thingy\": 47 }", new JsonDict { { "thingy", 47 } });
            assertDictParseSucc("{ \"thingy\": 47, \"bla\": null, \"sub\": { \"wah\": \"gah\" }, \"more\": true }", new JsonDict { { "more", true }, { "sub", new JsonDict { { "wah", "gah" } } }, { "bla", null }, { "thingy", 47.0m } });

            assertDictParseFail("");
            assertDictParseFail("null");
            assertDictParseFail("47");
            assertDictParseFail("{null: 47}");
            assertDictParseFail("{47: 47}");
            assertDictParseFail("{[]: 47}");
            assertDictParseFail("{47}");
            assertDictParseFail("{\"thing\": 47: 56}");
            assertDictParseFail("{\"thing\": 47");
            assertDictParseFail("\"thing\": 47}");
            assertDictParseFail("\"thing\": 47");
            assertDictParseFail("{\"thing\":");
            assertDictParseFail("{\"thing\"");
            assertDictParseFail("{\"thing\": 47  \"moar\": 56}");
            assertDictParseFail("{\"thing\": 47, ,  \"moar\": 56}");
            assertDictParseFail("{\"thing\": 47,  \"moar\": 56,}");
            assertDictParseFail("{,\"thing\": 47,  \"moar\": 56}");
        }

        [Test]
        public void TestJsonParseWhitespace()
        {
            assertBoolParseSucc("   true    ", true);
            assertStringParseSucc("     \"  thingy  \"   ", "  thingy  ");
            assertNumberParseSucc("     -47.3e-5    ", -47.3e-5, null);
            assertListParseSucc("    [25]    ", new JsonList { 25 });
            assertDictParseSucc("    {    \" here \"    :     25   }     ", new JsonDict { { " here ", 25 } });
        }

        private void assertBoolParseSucc(string json, bool val)
        {
            var t = JsonBool.Parse(json);
            Assert.AreEqual(val, (bool) t);
            Assert.IsTrue(JsonBool.TryParse(json, out t));
            Assert.AreEqual(val, (bool) t);
        }

        private void assertBoolParseFail(string json)
        {
            Assert.Throws<JsonParseException>(() => JsonBool.Parse(json));
            JsonBool val;
            Assert.IsFalse(JsonBool.TryParse(json, out val));
            Assert.IsNull(val);
        }

        private void assertStringParseSucc(string json, string val)
        {
            var t = JsonString.Parse(json);
            Assert.AreEqual(val, (string) t);
            Assert.IsTrue(JsonString.TryParse(json, out t));
            Assert.AreEqual(val, (string) t);
        }

        private void assertStringParseFail(string json)
        {
            Assert.Throws<JsonParseException>(() => JsonString.Parse(json));
            JsonString val;
            Assert.IsFalse(JsonString.TryParse(json, out val));
            Assert.IsNull(val);
        }

        private void assertNumberParseSucc(string json, double? valDo, decimal? valDe)
        {
            var t = JsonNumber.Parse(json);
            if (valDo != null)
                Assert.AreEqual(valDo.Value, (double) t);
            if (valDe != null)
                Assert.AreEqual(valDe.Value, (decimal) t);
            Assert.IsTrue(JsonNumber.TryParse(json, out t));
            if (valDo != null)
                Assert.AreEqual(valDo.Value, (double) t);
            if (valDe != null)
                Assert.AreEqual(valDe.Value, (decimal) t);
        }

        private void assertNumberParseSucc(string json, int val)
        {
            var t = JsonNumber.Parse(json);
            Assert.AreEqual(val, (int) t);
            Assert.AreEqual((double) val, (double) t);
            Assert.AreEqual((decimal) val, (decimal) t);
            Assert.IsTrue(JsonNumber.TryParse(json, out t));
            Assert.AreEqual(val, (int) t);
            Assert.AreEqual((double) val, (double) t);
            Assert.AreEqual((decimal) val, (decimal) t);
        }

        private void assertNumberParseFail(string json)
        {
            Assert.Throws<JsonParseException>(() => JsonNumber.Parse(json));
            JsonNumber val;
            Assert.IsFalse(JsonNumber.TryParse(json, out val));
            Assert.IsNull(val);
        }

        private void assertListParseSucc(string json, JsonList expected)
        {
            var t = JsonList.Parse(json);
            Assert.IsTrue(expected.Equals(t));
            Assert.IsTrue(JsonList.TryParse(json, out t));
            Assert.IsTrue(expected.Equals(t));
        }

        private void assertListParseFail(string json)
        {
            Assert.Throws<JsonParseException>(() => JsonList.Parse(json));
            JsonList val;
            Assert.IsFalse(JsonList.TryParse(json, out val));
            Assert.IsNull(val);
        }

        private void assertDictParseSucc(string json, JsonDict expected)
        {
            var t = JsonDict.Parse(json);
            Assert.IsTrue(expected.Equals(t));
            Assert.IsTrue(JsonDict.TryParse(json, out t));
            Assert.IsTrue(expected.Equals(t));
        }

        private void assertDictParseFail(string json)
        {
            Assert.Throws<JsonParseException>(() => JsonDict.Parse(json));
            JsonDict val;
            Assert.IsFalse(JsonDict.TryParse(json, out val));
            Assert.IsNull(val);
        }

        [Test]
        public void TestJsonComplexAndToString()
        {
            var parsed = JsonValue.Parse("{ \"thingy\": 47, \"bla\": null, \"sub\": { \"wah\": \"gah\", \"em\": [], \"fu\": [1, null, { \"wow\": {} }, \"2\"] }, \"more\": true }");
            var constructed = new JsonDict
            {
                { "thingy", 47 },
                { "bla", null },
                { "sub", new JsonDict
                {
                    { "wah", "gah" },
                    { "em", new JsonList{}},
                    { "fu", new JsonList { 1, null, new JsonDict { { "wow", new JsonDict {} } }, "2" } }
                }},
                { "more", true }
            };
            Assert.IsTrue(parsed.Equals(constructed));
            Assert.IsTrue(parsed.Equals(JsonValue.Parse(parsed.ToString())));
            Assert.IsTrue(constructed.Equals(JsonValue.Parse(constructed.ToString())));
        }
    }
}

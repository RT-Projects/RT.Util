using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using RT.Util;

namespace RT.KitchenSink.Collections.Tests
{
    /// <summary>
    /// Overview of RVariant testing: the idea is that we first bootstrap the
    /// process by creating a number of various kinds of values and manually
    /// checking their correctness (by inspecting the internal state). We then
    /// use the knowledge that this is correct to test various primitives,
    /// such as equality comparison and cloning. Then we can assume that equality
    /// is trustworthy and base other tests on the assumption that comparisons work.
    /// </summary>
    [TestFixture]
    public sealed class RVariantTests
    {
        #region Tests which construct various kinds of structures and verify that they were constructed correctly (incl Path)

        RVariant valStub;
        RVariant valInt;
        RVariant valString;
        RVariant valTrueBool;
        RVariant valFalseBool;
        RVariant valOneLevelDict;
        RVariant valOneLevelList;
        RVariant valComplex;

        [TestFixtureSetUp]
        public void InitAll()
        {
            valStub = new RVariant();

            valTrueBool = true;
            valFalseBool = false;
            valInt = 47;
            valString = "47fs";

            valOneLevelDict = new RVariant();
            valOneLevelDict["aString"] = "stuff";
            valOneLevelDict["anInt"] = 12345;
            valOneLevelDict["aTrueBool"] = true;
            valOneLevelDict["aFalseBool"] = false;

            valOneLevelList = new RVariant();
            valOneLevelList[0] = "zero";
            valOneLevelList[1] = "mind the GAP:";
            valOneLevelList[3] = 333;
            valOneLevelList[4] = true;
            valOneLevelList[5] = false;

            valComplex = new RVariant();
            valComplex["arr"][0]["dict"] = valOneLevelDict;
            valComplex["arr"][0]["list"] = valOneLevelList;
            valComplex["arr"][1]["dict"] = valOneLevelDict;
            valComplex["arr"][1]["list"] = valOneLevelList;
            valComplex["arr"][2] = valComplex["arr"][1];

            valComplex["sub1"]["stuff"] = "stuff";   // to be overwritten on the next line
            valComplex["sub1"] = valComplex["arr"];
            valComplex["sub1"][3] = 123;
            valComplex["sub1"][4] = 1234;

            valComplex["sub2"] = valComplex["sub1"];
            valComplex["sub2"][4] = valInt;
            valComplex["sub2"][4] = 4321;        // if the copying is wrong this could modify the value of valInt
            valComplex["sub2"][5] = valStub;
            valComplex["sub2"][6] = valTrueBool;
            valComplex["sub2"][7] = valFalseBool;
            valComplex["sub2"][8] = valInt;
            valComplex["sub2"][9] = valString;
        }

        [Test]
        public void TestStub()
        {
            Assert_KindAndPathMatch(valStub, RVariantKind.Stub, null);
            Assert.AreEqual(0, valStub.Count);
        }

        [Test]
        public void TestBasicValue()
        {
            /// This test does not provide much coverage but helps debug basic
            /// issues with the values and the test helper methods

            Assert.AreEqual(1, valTrueBool.Count);
            Assert.AreEqual(1, valFalseBool.Count);
            Assert.AreEqual(1, valInt.Count);
            Assert.AreEqual(1, valString.Count);

            Assert_ValueAndPathMatch(valTrueBool, true, null);
            Assert_ValueAndPathMatch(valFalseBool, false, null);
            Assert_ValueAndPathMatch(valInt, 47, null);
            Assert_ValueAndPathMatch(valString, "47fs", null);
        }

        [Test]
        public void TestOneLevelDict()
        {
            Assert.AreEqual(4, valOneLevelDict.Count);
            TestOneLevelDict(valOneLevelDict, "");
        }

        public void TestOneLevelDict(RVariant dict, string path)
        {
            Assert_KindAndPathMatch(dict, RVariantKind.Dict, path);

            Assert_ValueAndPathMatch(dict["aString"], "stuff", path + "/aString");
            Assert_ValueAndPathMatch(dict["anInt"], 12345, path + "/anInt");
            Assert_ValueAndPathMatch(dict["aTrueBool"], true, path + "/aTrueBool");
            Assert_ValueAndPathMatch(dict["aFalseBool"], false, path + "/aFalseBool");
        }

        [Test]
        public void TestOneLevelList()
        {
            Assert.AreEqual(6, valOneLevelList.Count);
            TestOneLevelList(valOneLevelList, "");
        }

        public void TestOneLevelList(RVariant list, string path)
        {
            Assert_KindAndPathMatch(list, RVariantKind.List, path);

            Assert_ValueAndPathMatch(list[0], "zero", path + "[0]");
            Assert_ValueAndPathMatch(list[1], "mind the GAP:", path + "[1]");
            Assert_KindAndPathMatch(list[2], RVariantKind.Stub, path + "[2]");
            Assert_ValueAndPathMatch(list[3], 333, path + "[3]");
            Assert_ValueAndPathMatch(list[4], true, path + "[4]");
            Assert_ValueAndPathMatch(list[5], false, path + "[5]");
        }

        [Test]
        public void TestComplexAndCopying()
        {
            // Check a few values directly to cover for the possibility that the
            // exhaustive check has a bug
            Assert_ValueAndPathMatch(valComplex["arr"][1]["list"][1], "mind the GAP:", "/arr[1]/list[1]");
            Assert_ValueAndPathMatch(valComplex["sub1"][2]["dict"]["anInt"], 12345, "/sub1[2]/dict/anInt");

            // Exhaustive check of every value
            TestComplexHelper(valComplex["arr"], "/arr");
            TestComplexHelper(valComplex["sub1"], "/sub1");
            TestComplexHelper(valComplex["sub2"], "/sub2");
            Assert_ValueAndPathMatch(valComplex["sub1"][3], 123, "/sub1[3]");
            Assert_ValueAndPathMatch(valComplex["sub1"][4], 1234, "/sub1[4]");
            Assert_ValueAndPathMatch(valComplex["sub2"][3], 123, "/sub2[3]");
            Assert_ValueAndPathMatch(valComplex["sub2"][4], 4321, "/sub2[4]");

            Assert_KindAndPathMatch(valComplex["sub2"][5], RVariantKind.Stub, "/sub2[5]");
            Assert_ValueAndPathMatch(valComplex["sub2"][6], true, "/sub2[6]");
            Assert_ValueAndPathMatch(valComplex["sub2"][7], false, "/sub2[7]");
            Assert_ValueAndPathMatch(valComplex["sub2"][8], 47, "/sub2[8]");
            Assert_ValueAndPathMatch(valComplex["sub2"][9], "47fs", "/sub2[9]");

            // Test both copying and equality in one go! (there's more coverage in other places too)
            Assert.IsTrue(valComplex == (RVariant) valComplex.Clone());
            Assert.IsTrue((RVariant) valComplex.Clone() == valComplex);
            Assert.IsFalse(valComplex != (RVariant) valComplex.Clone());
            Assert.IsFalse((RVariant) valComplex.Clone() != valComplex);
        }

        #region Helper methods

        public void Assert_ValueAndPathMatch(RVariant variant, bool val, string fullPath)
        {
            Assert_KindAndPathMatch(variant, RVariantKind.Value, fullPath);
            Assert.IsTrue(CheckValueEquals(variant, val));
        }

        public void Assert_ValueAndPathMatch(RVariant variant, int val, string fullPath)
        {
            Assert_KindAndPathMatch(variant, RVariantKind.Value, fullPath);
            Assert.IsTrue(CheckValueEquals(variant, val));
        }

        public void Assert_ValueAndPathMatch(RVariant variant, string val, string fullPath)
        {
            Assert_KindAndPathMatch(variant, RVariantKind.Value, fullPath);
            Assert.IsTrue(CheckValueEquals(variant, val));
        }

        public void Assert_KindAndPathMatch(RVariant variant, RVariantKind kind, string fullPath)
        {
            Assert.AreEqual(variant.Kind, kind);
            Assert.AreEqual(variant.FullPath, fullPath);
        }

        public void TestComplexHelper(RVariant variant, string path)
        {
            TestOneLevelDict(variant[0]["dict"], path + "[0]/dict");
            TestOneLevelList(variant[0]["list"], path + "[0]/list");
            TestOneLevelDict(variant[1]["dict"], path + "[1]/dict");
            TestOneLevelList(variant[1]["list"], path + "[1]/list");
            TestOneLevelDict(variant[2]["dict"], path + "[2]/dict");
            TestOneLevelList(variant[2]["list"], path + "[2]/list");
        }

        private bool BoolResultsIfConsistent(List<bool> results)
        {
            bool result = results[0]; // always expect at least one item
            for (int i = 1; i < results.Count; i++)
                Assert.AreEqual(result, results[i]);
            return result;
        }

        #region CheckValueEquals

        // Argh the C# generics are so limited... All this copy and pasting
        // just because "macros are evil"... </rant>

        public bool CheckValueEquals(RVariant variant, bool val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((bool) variant == val);
            res.Add(val == (bool) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((bool) variant != val));
            res.Add(!(val != (bool) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to bool, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing bool to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, byte val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((byte) variant == val);
            res.Add(val == (byte) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((byte) variant != val));
            res.Add(!(val != (byte) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to byte, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing byte to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, sbyte val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((sbyte) variant == val);
            res.Add(val == (sbyte) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((sbyte) variant != val));
            res.Add(!(val != (sbyte) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to sbyte, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing sbyte to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, short val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((short) variant == val);
            res.Add(val == (short) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((short) variant != val));
            res.Add(!(val != (short) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to short, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing short to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, ushort val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((ushort) variant == val);
            res.Add(val == (ushort) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((ushort) variant != val));
            res.Add(!(val != (ushort) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to ushort, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing ushort to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, int val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((int) variant == val);
            res.Add(val == (int) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((int) variant != val));
            res.Add(!(val != (int) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to int, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing int to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, uint val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((uint) variant == val);
            res.Add(val == (uint) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((uint) variant != val));
            res.Add(!(val != (uint) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to uint, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing uint to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, long val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((long) variant == val);
            res.Add(val == (long) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((long) variant != val));
            res.Add(!(val != (long) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to long, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing long to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, ulong val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((ulong) variant == val);
            res.Add(val == (ulong) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((ulong) variant != val));
            res.Add(!(val != (ulong) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to ulong, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing ulong to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, DateTime val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((DateTime) variant == val);
            res.Add(val == (DateTime) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((DateTime) variant != val));
            res.Add(!(val != (DateTime) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to DateTime, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing DateTime to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, float val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((float) variant == val);
            res.Add(val == (float) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((float) variant != val));
            res.Add(!(val != (float) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to float, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing float to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, double val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((double) variant == val);
            res.Add(val == (double) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((double) variant != val));
            res.Add(!(val != (double) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to double, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing double to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, decimal val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((decimal) variant == val);
            res.Add(val == (decimal) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((decimal) variant != val));
            res.Add(!(val != (decimal) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to decimal, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing decimal to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, char val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((char) variant == val);
            res.Add(val == (char) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((char) variant != val));
            res.Add(!(val != (char) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to char, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing char to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        public bool CheckValueEquals(RVariant variant, string val)
        {
            Assert.AreEqual(RVariantKind.Value, variant.Kind);
            List<bool> res = new List<bool>();
            // Operator ==
            res.Add(variant == (RVariant) val);
            res.Add((RVariant) val == variant);
            res.Add((string) variant == val);
            res.Add(val == (string) variant);
            // Operator !=
            res.Add(!(variant != (RVariant) val));
            res.Add(!((RVariant) val != variant));
            res.Add(!((string) variant != val));
            res.Add(!(val != (string) variant));
            // Method Equals
            res.Add(variant.Equals((RVariant) val));
            res.Add(variant.Equals((object) val));
            res.Add(val.Equals(variant)); // must be cast to string, not passed as an object
            Assert.IsFalse(val.Equals((object) variant)); // comparing string to non-boxed object is always false
            // Done - see if all values were equal
            return BoolResultsIfConsistent(res);
        }

        #endregion

        #endregion

        #endregion

        [Test]
        public void TestImplicitCastAndEquality()
        {
            // Convert from value to RVariant
            RVariant boolTrueM = true;
            RVariant boolFalseM = false;
            RVariant byteM = (byte) 250;
            RVariant sbyteM = (sbyte) -123;
            RVariant shortM = (short) -26520;
            RVariant ushortM = (ushort) 64928;
            RVariant intM = (int) -239749639;
            RVariant uintM = (uint) 982739723;
            RVariant longM = (long) -2379847293864876;
            RVariant ulongM = (ulong) 3402938479823746;
            RVariant floatM = (float) 3.1415925f;
            RVariant doubleM = (double) 1208.1287987986866d;
            RVariant decimalM = (decimal) 198729384734.239472736876m;
            RVariant DateTimeM = new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc);
            RVariant charM = '\u0065';
            RVariant stringM = "stuff";

            // Check that all the values store the object as-is
            Assert_StoresCorrectValueOfCorrectType(boolTrueM, true, TypeCode.Boolean);
            Assert_StoresCorrectValueOfCorrectType(boolFalseM, false, TypeCode.Boolean);
            Assert_StoresCorrectValueOfCorrectType(byteM, (byte) 250, TypeCode.Byte);
            Assert_StoresCorrectValueOfCorrectType(sbyteM, (sbyte) -123, TypeCode.SByte);
            Assert_StoresCorrectValueOfCorrectType(shortM, (short) -26520, TypeCode.Int16);
            Assert_StoresCorrectValueOfCorrectType(ushortM, (ushort) 64928, TypeCode.UInt16);
            Assert_StoresCorrectValueOfCorrectType(intM, (int) -239749639, TypeCode.Int32);
            Assert_StoresCorrectValueOfCorrectType(uintM, (uint) 982739723, TypeCode.UInt32);
            Assert_StoresCorrectValueOfCorrectType(longM, (long) -2379847293864876, TypeCode.Int64);
            Assert_StoresCorrectValueOfCorrectType(ulongM, (ulong) 3402938479823746, TypeCode.UInt64);
            Assert_StoresCorrectValueOfCorrectType(floatM, 3.1415925f, TypeCode.Single);
            Assert_StoresCorrectValueOfCorrectType(doubleM, 1208.1287987986866d, TypeCode.Double);
            Assert_StoresCorrectValueOfCorrectType(decimalM, 198729384734.239472736876m, TypeCode.Decimal);
            Assert_StoresCorrectValueOfCorrectType(DateTimeM, new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc), TypeCode.DateTime);
            Assert_StoresCorrectValueOfCorrectType(charM, '\u0065', TypeCode.Char);
            Assert_StoresCorrectValueOfCorrectType(stringM, "stuff", TypeCode.String);
            // And that equality checks work and produce consistent results
            Assert.IsTrue(CheckValueEquals(boolTrueM, true));
            Assert.IsTrue(CheckValueEquals(boolFalseM, false));
            Assert.IsTrue(CheckValueEquals(byteM, (byte) 250));
            Assert.IsTrue(CheckValueEquals(sbyteM, (sbyte) -123));
            Assert.IsTrue(CheckValueEquals(shortM, (short) -26520));
            Assert.IsTrue(CheckValueEquals(ushortM, (ushort) 64928));
            Assert.IsTrue(CheckValueEquals(intM, (int) -239749639));
            Assert.IsTrue(CheckValueEquals(uintM, (uint) 982739723));
            Assert.IsTrue(CheckValueEquals(longM, (long) -2379847293864876));
            Assert.IsTrue(CheckValueEquals(ulongM, (ulong) 3402938479823746));
            Assert.IsTrue(CheckValueEquals(floatM, 3.1415925f));
            Assert.IsTrue(CheckValueEquals(doubleM, 1208.1287987986866d));
            Assert.IsTrue(CheckValueEquals(decimalM, 198729384734.239472736876m));
            Assert.IsTrue(CheckValueEquals(DateTimeM, new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc)));
            Assert.IsTrue(CheckValueEquals(charM, '\u0065'));
            Assert.IsTrue(CheckValueEquals(stringM, "stuff"));

            // Convert from RVariant to value
            bool boolTrueV = boolTrueM;
            bool boolFalseV = boolFalseM;
            byte byteV = byteM;
            sbyte sbyteV = sbyteM;
            short shortV = shortM;
            ushort ushortV = ushortM;
            int intV = intM;
            uint uintV = uintM;
            long longV = longM;
            ulong ulongV = ulongM;
            float floatV = floatM;
            double doubleV = doubleM;
            decimal decimalV = decimalM;
            DateTime DateTimeV = DateTimeM;
            char charV = charM;
            string stringV = stringM;

            // Check that all the values are still correct
            Assert.AreEqual(boolTrueV, true);
            Assert.AreEqual(boolFalseV, false);
            Assert.AreEqual(byteV, (byte) 250);
            Assert.AreEqual(sbyteV, (sbyte) -123);
            Assert.AreEqual(shortV, (short) -26520);
            Assert.AreEqual(ushortV, (ushort) 64928);
            Assert.AreEqual(intV, (int) -239749639);
            Assert.AreEqual(uintV, (uint) 982739723);
            Assert.AreEqual(longV, (long) -2379847293864876);
            Assert.AreEqual(ulongV, (ulong) 3402938479823746);
            Assert.AreEqual(floatV, 3.1415925f);
            Assert.AreEqual(doubleV, 1208.1287987986866d);
            Assert.AreEqual(decimalV, 198729384734.239472736876m);
            Assert.AreEqual(DateTimeV, new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc));
            Assert.AreEqual(charV, '\u0065');
            Assert.AreEqual(stringV, "stuff");


            // Convert to string
            boolTrueM = (string) boolTrueM;
            boolFalseM = (string) boolFalseM;
            byteM = (string) byteM;
            sbyteM = (string) sbyteM;
            shortM = (string) shortM;
            ushortM = (string) ushortM;
            intM = (string) intM;
            uintM = (string) uintM;
            longM = (string) longM;
            ulongM = (string) ulongM;
            floatM = (string) floatM;
            doubleM = (string) doubleM;
            decimalM = (string) decimalM;
            DateTimeM = (string) DateTimeM;
            charM = (string) charM;
            stringM = (string) stringM;

            // Check that they are now strings
            Assert_StoresCorrectValueOfCorrectType(boolTrueM, "True", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(boolFalseM, "False", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(byteM, "250", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(sbyteM, "-123", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(shortM, "-26520", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(ushortM, "64928", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(intM, "-239749639", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(uintM, "982739723", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(longM, "-2379847293864876", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(ulongM, "3402938479823746", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(floatM, "3.1415925", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(doubleM, "1208.1287987986866", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(decimalM, "198729384734.239472736876", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(DateTimeM, "2008-12-30 21:32:56.9880000Z", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(charM, "\u0065", TypeCode.String);
            Assert_StoresCorrectValueOfCorrectType(stringM, "stuff", TypeCode.String);
            // And that equality checks still work and produce consistent results
            Assert.IsTrue(CheckValueEquals(boolTrueM, true));
            Assert.IsTrue(CheckValueEquals(boolFalseM, false));
            Assert.IsTrue(CheckValueEquals(byteM, (byte) 250));
            Assert.IsTrue(CheckValueEquals(sbyteM, (sbyte) -123));
            Assert.IsTrue(CheckValueEquals(shortM, (short) -26520));
            Assert.IsTrue(CheckValueEquals(ushortM, (ushort) 64928));
            Assert.IsTrue(CheckValueEquals(intM, (int) -239749639));
            Assert.IsTrue(CheckValueEquals(uintM, (uint) 982739723));
            Assert.IsTrue(CheckValueEquals(longM, (long) -2379847293864876));
            Assert.IsTrue(CheckValueEquals(ulongM, (ulong) 3402938479823746));
            Assert.IsTrue(CheckValueEquals(floatM, 3.1415925f));
            Assert.IsTrue(CheckValueEquals(doubleM, 1208.1287987986866d));
            Assert.IsTrue(CheckValueEquals(decimalM, 198729384734.239472736876m));
            Assert.IsTrue(CheckValueEquals(DateTimeM, new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc)));
            Assert.IsTrue(CheckValueEquals(charM, '\u0065'));
            Assert.IsTrue(CheckValueEquals(stringM, "stuff"));

            // Convert from RVariant to value
            boolTrueV = boolTrueM;
            boolFalseV = boolFalseM;
            byteV = byteM;
            sbyteV = sbyteM;
            shortV = shortM;
            ushortV = ushortM;
            intV = intM;
            uintV = uintM;
            longV = longM;
            ulongV = ulongM;
            floatV = floatM;
            doubleV = doubleM;
            decimalV = decimalM;
            DateTimeV = DateTimeM;
            charV = charM;
            stringV = stringM;

            // Check that all the values are still correct
            Assert.AreEqual(boolTrueV, true);
            Assert.AreEqual(boolFalseV, false);
            Assert.AreEqual(byteV, (byte) 250);
            Assert.AreEqual(sbyteV, (sbyte) -123);
            Assert.AreEqual(shortV, (short) -26520);
            Assert.AreEqual(ushortV, (ushort) 64928);
            Assert.AreEqual(intV, (int) -239749639);
            Assert.AreEqual(uintV, (uint) 982739723);
            Assert.AreEqual(longV, (long) -2379847293864876);
            Assert.AreEqual(ulongV, (ulong) 3402938479823746);
            Assert.AreEqual(floatV, 3.1415925f);
            Assert.AreEqual(doubleV, 1208.1287987986866d);
            Assert.AreEqual(decimalV, 198729384734.239472736876m);
            Assert.AreEqual(DateTimeV, new DateTime(2008, 12, 30, 21, 32, 56, 988, DateTimeKind.Utc));
            Assert.AreEqual(charV, '\u0065');
            Assert.AreEqual(stringV, "stuff");


            // Now check that inequality tests are also correct and consistent
            Assert.IsFalse(CheckValueEquals(boolTrueM, false));
            Assert.IsFalse(CheckValueEquals(boolFalseM, true));
            Assert.IsFalse(CheckValueEquals(byteM, (byte) 251));
            Assert.IsFalse(CheckValueEquals(sbyteM, (sbyte) -124));
            Assert.IsFalse(CheckValueEquals(shortM, (short) -26521));
            Assert.IsFalse(CheckValueEquals(ushortM, (ushort) 64923));
            Assert.IsFalse(CheckValueEquals(intM, (int) -239749632));
            Assert.IsFalse(CheckValueEquals(uintM, (uint) 982739724));
            Assert.IsFalse(CheckValueEquals(longM, (long) -2379847293864875));
            Assert.IsFalse(CheckValueEquals(ulongM, (ulong) 3402938479823745));
            Assert.IsFalse(CheckValueEquals(floatM, 3.1415929f));
            Assert.IsFalse(CheckValueEquals(doubleM, 1208.1287987986869d));
            Assert.IsFalse(CheckValueEquals(decimalM, 198729384734.239472736879m));
            Assert.IsFalse(CheckValueEquals(DateTimeM, new DateTime(2008, 12, 30, 21, 32, 56, 989, DateTimeKind.Utc)));
            Assert.IsFalse(CheckValueEquals(charM, '\u0064'));
            Assert.IsFalse(CheckValueEquals(stringM, "stuffed"));


            // Test equality of stubs and nulls
            Assert.IsFalse(valInt == null);
            Assert.IsFalse(null == valInt);
            Assert.IsFalse(valStub == valInt);
            Assert.IsFalse(valInt == valStub);

            Assert.IsFalse(valStub == null);
            Assert.IsTrue(valStub == (RVariant) valStub.Clone());
            Assert.IsTrue(valInt == (RVariant) valInt.Clone());
        }

        [Test]
        public void TestXmlAndComplexEquality()
        {
            string rootName;
            RVariant roundtripped;

            roundtripped = new RVariant(GetRoundTrippedXml(valStub, "valStub"), out rootName);
            Assert.AreEqual("valStub", rootName);
            Assert_RVariantsAreEqual(valStub, roundtripped);

            roundtripped = new RVariant(GetRoundTrippedXml(valOneLevelList, "valOneLevelList"), out rootName);
            Assert.AreEqual("valOneLevelList", rootName);
            Assert_RVariantsAreEqual(valOneLevelList, roundtripped);

            roundtripped = new RVariant(GetRoundTrippedXml(valOneLevelDict, "valOneLevelDict"), out rootName);
            Assert.AreEqual("valOneLevelDict", rootName);
            Assert_RVariantsAreEqual(valOneLevelDict, roundtripped);

            roundtripped = new RVariant(GetRoundTrippedXml(valComplex, "valComplex"), out rootName);
            Assert.AreEqual("valComplex", rootName);
            Assert_RVariantsAreEqual(valComplex, roundtripped);

            // Complex XML test - add each of the above to a separate xml element
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(xml.CreateElement("test"));
            XmlElement elStub = xml.CreateElement("stub");
            XmlElement elOneLevelList = xml.CreateElement("oneLevelList");
            XmlElement elOneLevelDict = xml.CreateElement("oneLevelDict");
            XmlElement elComplex = xml.CreateElement("complex");
            xml.DocumentElement.AppendChild(elStub);
            xml.DocumentElement.AppendChild(elOneLevelList);
            xml.DocumentElement.AppendChild(elOneLevelDict);
            xml.DocumentElement.AppendChild(elComplex);
            valStub.ToXml(elStub);
            valOneLevelList.ToXml(elOneLevelList);
            valOneLevelDict.ToXml(elOneLevelDict);
            valComplex.ToXml(elComplex);

            byte[] xmlRaw;
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = new XmlTextWriter(ms, Encoding.UTF8))
                    xml.WriteTo(xw);
                xmlRaw = ms.ToArray();
            }

            xml = new XmlDocument();
            xml.Load(new MemoryStream(xmlRaw));
            elStub = elOneLevelList = elOneLevelDict = elComplex = null;

            // what a fugly long-winded way of accessing xml elements...
            // good thing there's RVariant!...
            elStub = (XmlElement) xml.DocumentElement.GetElementsByTagName("stub")[0];
            elOneLevelList = (XmlElement) xml.DocumentElement.GetElementsByTagName("oneLevelList")[0];
            elOneLevelDict = (XmlElement) xml.DocumentElement.GetElementsByTagName("oneLevelDict")[0];
            elComplex = (XmlElement) xml.DocumentElement.GetElementsByTagName("complex")[0];
            Assert_RVariantsAreEqual(valStub, new RVariant(elStub));
            Assert_RVariantsAreEqual(valOneLevelList, new RVariant(elOneLevelList));
            Assert_RVariantsAreEqual(valOneLevelDict, new RVariant(elOneLevelDict));
            Assert_RVariantsAreEqual(valComplex, new RVariant(elComplex));
        }

        private void Assert_RVariantsAreEqual(RVariant v1, RVariant v2)
        {
            Assert.IsTrue(v1.Equals(v2));
            Assert.IsTrue(v2.Equals(v1));
            Assert.IsTrue(v1 == v2);
            Assert.IsTrue(v2 == v1);
            Assert.IsFalse(v1 != v2);
            Assert.IsFalse(v2 != v1);
        }

        private XmlDocument GetRoundTrippedXml(RVariant variant, string rootName)
        {
            XmlDocument xml = variant.ToXml(rootName);
            byte[] xmlRaw;
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = new XmlTextWriter(ms, Encoding.UTF8))
                    xml.WriteTo(xw);
                xmlRaw = ms.ToArray();
            }
            xml = new XmlDocument();
            xml.Load(new MemoryStream(xmlRaw));
            return xml;
        }

        [Test]
        public void TestDefaultTo()
        {
            RVariant mv = new RVariant();
            int i;
            i = mv["stuff"].OrDefaultTo(20);
            Assert.AreEqual(20, i);
        }

        [Test]
        public void TestExceptions()
        {
            // Invalid indexing
            try { valInt[3] = 3; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { valInt["s"] = 3; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { valOneLevelDict[3] = 3; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { valOneLevelList["s"] = 3; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            // Invalid implicit casts
            try { int i = valStub; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { int i = valOneLevelList; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { int i = valOneLevelDict; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { bool b = valInt; Assert.Fail(); }
            catch (RVariantConvertException) { }

            // Other exceptions
            try { object o = valOneLevelList.Value; Assert.Fail(); }
            catch (RVariantNotFoundException) { }

            try { valOneLevelList[0] = null; Assert.Fail(); }
            catch (RVariantException) { }

            try { valOneLevelDict["a"] = null; Assert.Fail(); }
            catch (RVariantException) { }
        }

        [Test]
        public void RealLifeTest()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<example kind='dict'><my kind='dict'><test kind='list'><item value='47'/></test></my></example>");
            RVariant mv = new RVariant(doc);

            int mytest0, myother;

            // Read an existing setting
            mytest0 = mv["my"]["test"][0];
            Assert.AreEqual(47, mytest0);

            // Read a non-existent setting
            try { myother = mv["my"]["other"][3]; }
            catch (RVariantNotFoundException) { }

            // Read a setting which is actually not a value but a list/dict
            try { myother = mv["my"]; }
            catch (RVariantNotFoundException) { }

            // Read existing setting with default
            mytest0 = mv["my"]["test"][0].OrDefaultTo(80);
            Assert.AreEqual(47, mytest0);

            // Read a non-existent setting with default
            myother = mv["my"]["other"][3].OrDefaultTo(80);
            Assert.AreEqual(80, myother);

            // Check the path
            Assert.AreEqual("/my/test[0]", mv["my"]["test"][0].FullPath);
        }

        #region Helper methods

        private void Assert_StoresCorrectValueOfCorrectType(RVariant mvalue, object expected, TypeCode code)
        {
            Assert.AreEqual(RVariantKind.Value, mvalue.Kind);
            Assert.AreEqual(code, ExactConvert.GetTypeCode(mvalue.Value));
            Assert.AreEqual(expected, mvalue.Value);
        }

        #endregion

        [Test]
        public void TestExists()
        {
            RVariant variant = new RVariant();

            Assert.IsFalse(variant.Exists);
            Assert.IsFalse(variant["stuff"].Exists);
            Assert.IsFalse(variant["array"][3].Exists);

            variant["stuff"][0] = 20;
            variant["array"][3] = "a";

            Assert.IsTrue(variant.Exists);
            Assert.IsTrue(variant["stuff"].Exists);
            Assert.IsTrue(variant["array"][3].Exists);
        }
    }
}

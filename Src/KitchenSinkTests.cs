using System;
using System.Reflection;
using NUnit.Framework;
using RT.Util;

namespace RT.KitchenSink.Tests
{
    static class KitchenSinkTests
    {
        static void Main(string[] args)
        {
            if (false)
            {
                Testing.GenerateTestingCode(@"..\..\main\common\KitchenSink\KitchenSinkTests.cs", "Run Tests", Assembly.GetExecutingAssembly().GetExportedTypes(),
                    typeof(TestFixtureAttribute), typeof(TestFixtureSetUpAttribute), typeof(TestAttribute), typeof(TestFixtureTearDownAttribute));
            }
            else
            {
                #region Run Tests
                Console.WriteLine("");
                Console.WriteLine("Testing type: RT.KitchenSink.Tests.DiffTests");
                var doDiffTests = new RT.KitchenSink.Tests.DiffTests();
                Console.WriteLine("-- Running test: DiffTest");
                doDiffTests.DiffTest();
                Console.WriteLine("-- Running test: DiffTestPredicate");
                doDiffTests.DiffTestPredicate();
                Console.WriteLine("-- Running test: DiffTestPostprocessor");
                doDiffTests.DiffTestPostprocessor();
                Console.WriteLine("");
                Console.WriteLine("Testing type: RT.KitchenSink.Collections.Tests.RVariantTests");
                var doRVariantTests = new RT.KitchenSink.Collections.Tests.RVariantTests();
                Console.WriteLine("-- Running setup: InitAll");
                doRVariantTests.InitAll();
                Console.WriteLine("-- Running test: TestStub");
                doRVariantTests.TestStub();
                Console.WriteLine("-- Running test: TestBasicValue");
                doRVariantTests.TestBasicValue();
                Console.WriteLine("-- Running test: TestOneLevelDict");
                doRVariantTests.TestOneLevelDict();
                Console.WriteLine("-- Running test: TestOneLevelList");
                doRVariantTests.TestOneLevelList();
                Console.WriteLine("-- Running test: TestComplexAndCopying");
                doRVariantTests.TestComplexAndCopying();
                Console.WriteLine("-- Running test: TestImplicitCastAndEquality");
                doRVariantTests.TestImplicitCastAndEquality();
                Console.WriteLine("-- Running test: TestXmlAndComplexEquality");
                doRVariantTests.TestXmlAndComplexEquality();
                Console.WriteLine("-- Running test: TestDefaultTo");
                doRVariantTests.TestDefaultTo();
                Console.WriteLine("-- Running test: TestExceptions");
                doRVariantTests.TestExceptions();
                Console.WriteLine("-- Running test: RealLifeTest");
                doRVariantTests.RealLifeTest();
                Console.WriteLine("-- Running test: TestExists");
                doRVariantTests.TestExists();
                Console.WriteLine("");
                Console.WriteLine("Testing type: RT.KitchenSink.Collections.Tests.SetTests");
                var doSetTests = new RT.KitchenSink.Collections.Tests.SetTests();
                Console.WriteLine("-- Running test: SetTests1");
                doSetTests.SetTests1();
                #endregion

                Console.WriteLine("");
                Console.WriteLine("Tests passed; press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}

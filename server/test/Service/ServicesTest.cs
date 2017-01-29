using System;
using System.Collections.Generic;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ServicesTest
    {
        static ProcedureCall Call (string service, string procedure, params Argument[] args)
        {
            var call = new ProcedureCall (service, procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return call;
        }

        static Argument Arg (uint position, object value)
        {
            return new Argument (position, value);
        }

        static ProcedureResult Run (ProcedureCall call)
        {
            var procedure = KRPC.Service.Services.Instance.GetProcedureSignature (call.Service, call.Procedure);
            return KRPC.Service.Services.Instance.ExecuteCall (procedure, call);
        }

        static void CheckResultNotEmpty (ProcedureResult result)
        {
            Assert.IsTrue (result.HasValue);
            Assert.IsFalse (result.HasError);
        }

        static void CheckResultEmpty (ProcedureResult result)
        {
            Assert.IsFalse (result.HasValue);
            Assert.IsFalse (result.HasError);
        }

        [SetUp]
        public void SetUp ()
        {
            CallContext.SetGameScene (GameScene.Flight);
        }

        [Test]
        public void NonExistantService ()
        {
            Assert.Throws<RPCException> (() => Run (Call ("NonExistantService", "NonExistantProcedure")));
        }

        [Test]
        public void NonExistantProcedure ()
        {
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "NonExistantProcedure")));
        }

        [Test]
        public void ProcedureWithoutAttribute ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureWithoutAttribute ());
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureWithoutAttribute")));
            mock.Verify (x => x.ProcedureWithoutAttribute (), Times.Never ());
        }

        [Test]
        public void ExecuteCallNoArgsNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureNoArgsNoReturn"));
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallSingleInvalidArgNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.CreateTestObject (It.IsAny<string> ()));
            // should pass a string, not an int
            var request = Call ("TestService", "CreateTestObject", Arg (0, 42));
            Assert.Throws<RPCException> (() => Run (request));
            mock.Verify (x => x.CreateTestObject (It.IsAny<string> ()), Times.Never ());
        }

        [Test]
        public void ExecuteCallNoArgsReturnsNull ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ((string)null);
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureNoArgsReturns")));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        [Test]
        public void ExecuteCallNoArgsThrows ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Throws (new ArgumentException ("test exception"));
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureNoArgsReturns")));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        [Test]
        public void ExecuteCallSingleArgNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<string> ()))
                .Callback ((string x) => Assert.AreEqual ("foo", x));
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureSingleArgNoReturn", Arg (0, "foo")));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<string> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallThreeArgsNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<string> (),
                It.IsAny<int> (),
                It.IsAny<string> ()))
                        .Callback ((string x,
                                    int y,
                                    string z) => {
                Assert.AreEqual ("foo", x);
                Assert.AreEqual (42, y);
                Assert.AreEqual ("bar", z);
            });
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureThreeArgsNoReturn",
                             Arg (0, "foo"), Arg (1, 42), Arg (2, "bar")));
            mock.Verify (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<string> (), It.IsAny<int> (), It.IsAny<string> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallNoArgsReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ("foo");
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureNoArgsReturns"));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.AreEqual ("foo", (string)result.Value);
        }

        [Test]
        public void ExecuteCallArgsReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<string> ()))
                .Returns ((string x) => x + "bar");
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureSingleArgReturns", Arg (0, "foo")));
            mock.Verify (x => x.ProcedureSingleArgReturns (It.IsAny<string> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.AreEqual ("foobar", (string)result.Value);
        }

        [Test]
        public void ExecuteCallForPropertyGetter ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.PropertyWithGet).Returns ("foo");
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "get_PropertyWithGet"));
            mock.Verify (x => x.PropertyWithGet, Times.Once ());
            CheckResultNotEmpty (result);
            Assert.AreEqual ("foo", (string)result.Value);
        }

        [Test]
        public void ExecuteCallForPropertySetter ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.SetupSet (x => x.PropertyWithSet = "foo");
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "set_PropertyWithSet", Arg (0, "foo")));
            mock.VerifySet (x => x.PropertyWithSet = "foo", Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallWithObjectReturn ()
        {
            var instance = new TestService.TestClass ("foo");
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.CreateTestObject ("foo")).Returns (instance);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "CreateTestObject", Arg (0, "foo")));
            mock.Verify (x => x.CreateTestObject (It.IsAny<string> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            Assert.AreEqual (instance, (TestService.TestClass)result.Value);
        }

        [Test]
        public void ExecuteCallWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("foo");
            ObjectStore.Instance.AddInstance (instance);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreSame (instance, x));
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "DeleteTestObject", Arg (0, instance)));
            mock.Verify (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallWithNullObjectParameterAndReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreEqual (null, x))
                .Returns ((TestService.TestClass x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoTestObject", Arg (0, null)));
            mock.Verify (x => x.EchoTestObject (It.IsAny<TestService.TestClass> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNull (result.Value);
        }

        [Test]
        public void ExecuteCallForObjectMethod ()
        {
            var instance = new TestService.TestClass ("jeb");
            const float arg = 3.14159f;
            var result = Run (Call ("TestService", "TestClass_FloatToString", Arg (0, instance), Arg (1, arg)));
            CheckResultNotEmpty (result);
            Assert.AreEqual ("jeb3.14159", (string)result.Value);
        }

        [Test]
        public void ExecuteCallForObjectMethodWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("bill");
            var arg = new TestService.TestClass ("bob");
            var result = Run (Call ("TestService", "TestClass_ObjectToString", Arg (0, instance), Arg (1, arg)));
            CheckResultNotEmpty (result);
            Assert.AreEqual ("billbob", (string)result.Value);
        }

        [Test]
        public void ExecuteCallForClassPropertyGetter ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            var result = Run (Call ("TestService", "TestClass_get_IntProperty", Arg (0, instance)));
            CheckResultNotEmpty (result);
            Assert.AreEqual (42, (int)result.Value);
        }

        [Test]
        public void ExecuteCallForClassPropertySetter ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            var result = Run (Call ("TestService", "TestClass_set_IntProperty", Arg (0, instance), Arg (1, 1337)));
            CheckResultEmpty (result);
            Assert.AreEqual (1337, instance.IntProperty);
        }

        [Test]
        public void ExecuteCallForClassStaticMethod ()
        {
            var result = Run (Call ("TestService", "TestClass_static_StaticMethod", Arg (0, "bob")));
            CheckResultNotEmpty (result);
            Assert.AreEqual ("jebbob", (string)result.Value);
        }

        [Test]
        public void ExecuteCallWithClassTypeParameterFromDifferentService ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            ObjectStore.Instance.AddInstance (instance);
            var result = Run (Call ("TestService2", "ClassTypeFromOtherServiceAsParameter", Arg (0, instance)));
            CheckResultNotEmpty (result);
            Assert.AreEqual (42, (int)result.Value);
        }

        [Test]
        public void ExecuteCallWithClassTypeReturnFromDifferentService ()
        {
            var result = Run (Call ("TestService2", "ClassTypeFromOtherServiceAsReturn", Arg (0, "jeb")));
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            var obj = (TestService.TestClass)result.Value;
            Assert.AreEqual ("jeb", obj.Value);
        }

        [Test]
        public void ExecuteCallSingleOptionalArgNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleOptionalArgNoReturn (It.IsAny<string> ()))
                .Callback ((string x) => Assert.AreEqual (x, "foo"));
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureSingleOptionalArgNoReturn"));
            mock.Verify (x => x.ProcedureSingleOptionalArgNoReturn (It.IsAny<string> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallThreeOptionalArgs ()
        {
            const float arg0 = 3.14159f;
            const int arg2 = 42;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<int> ()))
                .Callback ((float x,
                            string y,
                            int z) => {
                Assert.AreEqual (arg0, x);
                Assert.AreEqual ("jeb", y);
                Assert.AreEqual (arg2, z);
            });
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureThreeOptionalArgsNoReturn",
                             Arg (2, arg2), Arg (0, arg0)));
            mock.Verify (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<int> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallOptionalNullArg ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureOptionalNullArg (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreEqual (x, null));
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureOptionalNullArg"));
            mock.Verify (x => x.ProcedureOptionalNullArg (It.IsAny<TestService.TestClass> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallMissingArgs ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (), It.IsAny<string> (), It.IsAny<int> ()));
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureThreeOptionalArgsNoReturn")));
            mock.Verify (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (), It.IsAny<string> (), It.IsAny<int> ()), Times.Never ());
        }

        [Test]
        public void ExecuteCallSingleEnumArgNoReturn ()
        {
            var arg = TestService.TestEnum.Y;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()))
                .Callback ((TestService.TestEnum x) => Assert.AreEqual (TestService.TestEnum.Y, x));
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureEnumArg", Arg (0, arg)));
            mock.Verify (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()), Times.Once ());
            CheckResultEmpty (result);
        }

        [Test]
        public void ExecuteCallNoArgEnumReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumReturn ()).Returns (TestService.TestEnum.Z);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "ProcedureEnumReturn"));
            mock.Verify (x => x.ProcedureEnumReturn (), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.AreEqual (TestService.TestEnum.Z, (TestService.TestEnum)result.Value);
        }

        [Test]
        public void ExecuteCallSingleInvalidEnumArgNoReturn ()
        {
            const int arg = 9999;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()));
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureTestEnumArg", Arg (0, arg))));
            mock.Verify (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()), Times.Never ());
        }

        int BlockingProcedureNoReturnFnCount;

        void BlockingProcedureNoReturnFn (int n)
        {
            BlockingProcedureNoReturnFnCount++;
            if (n == 0)
                return;
            throw new YieldException (new ParameterizedContinuationVoid<int> (BlockingProcedureNoReturnFn, n - 1));
        }

        int BlockingProcedureReturnsFnCount;

        int BlockingProcedureReturnsFn (int n, int sum)
        {
            BlockingProcedureReturnsFnCount++;
            if (n == 0)
                return sum;
            throw new YieldException (new ParameterizedContinuation<int,int,int> (BlockingProcedureReturnsFn, n - 1, sum + n));
        }

        [Test]
        public void HandleBlockingRequestArgsNoReturn ()
        {
            const int num = 42;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureNoReturn (It.IsAny<int> ()))
                .Callback ((int n) => BlockingProcedureNoReturnFn (n));
            TestService.Service = mock.Object;
            var call = Call ("TestService", "BlockingProcedureNoReturn", Arg (0, num));
            BlockingProcedureNoReturnFnCount = 0;
            ProcedureResult result = null;
            Continuation<ProcedureResult> continuation = new ProcedureCallContinuation (call);
            while (result == null) {
                try {
                    result = continuation.Run ();
                } catch (YieldException e) {
                    continuation = (Continuation<ProcedureResult>)e.Continuation;
                }
            }
            // Verify the procedure is called once, but the handler function is called multiple times
            mock.Verify (x => x.BlockingProcedureNoReturn (It.IsAny<int> ()), Times.Once ());
            Assert.AreEqual (num + 1, BlockingProcedureNoReturnFnCount);
            CheckResultEmpty (result);
        }

        [Test]
        public void HandleBlockingRequestArgsReturns ()
        {
            const int num = 10;
            const int expectedResult = 55;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => BlockingProcedureReturnsFn (n, sum));
            TestService.Service = mock.Object;
            var call = Call ("TestService", "BlockingProcedureReturns", Arg (0, num));
            BlockingProcedureReturnsFnCount = 0;
            ProcedureResult result = null;
            Continuation<ProcedureResult> continuation = new ProcedureCallContinuation (call);
            while (result == null) {
                try {
                    result = continuation.Run ();
                } catch (YieldException e) {
                    continuation = (Continuation<ProcedureResult>)e.Continuation;
                }
            }
            // Verify the KRPCProcedure is called once, but the handler function is called multiple times
            mock.Verify (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()), Times.Once ());
            Assert.AreEqual (num + 1, BlockingProcedureReturnsFnCount);
            CheckResultNotEmpty (result);
            Assert.AreEqual (expectedResult, (int)result.Value);
        }

        [Test]
        public void HandleEchoList ()
        {
            var list = new List<string> { "jeb", "bob", "bill" };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ())).Returns ((IList<string> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoList", Arg (0, list)));
            mock.Verify (x => x.EchoList (It.IsAny<IList<string>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            CollectionAssert.AreEqual (list, (IList<string>)result.Value);
        }

        [Test]
        public void HandleEchoDictionary ()
        {
            var dictionary = new Dictionary<int,string> { { 0, "jeb" }, { 1, "bob" }, { 2, "bill" } };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()))
                .Returns ((IDictionary<int,string> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoDictionary", Arg (0, dictionary)));
            mock.Verify (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            CollectionAssert.AreEquivalent (dictionary, (IDictionary<int,string>)result.Value);
        }

        [Test]
        public void HandleEchoSet ()
        {
            var set = new HashSet<int> { 345, 723, 112 };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoSet (It.IsAny<HashSet<int>> ())).Returns ((HashSet<int> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoSet", Arg (0, set)));
            mock.Verify (x => x.EchoSet (It.IsAny<HashSet<int>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            CollectionAssert.AreEqual (set, (HashSet<int>)result.Value);
        }

        [Test]
        public void HandleEchoTuple ()
        {
            var tuple = KRPC.Utils.Tuple.Create (42, false);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()))
                .Returns ((KRPC.Utils.Tuple<int,bool> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoTuple", Arg (0, tuple)));
            mock.Verify (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            Assert.AreEqual (tuple, (KRPC.Utils.Tuple<int,bool>)result.Value);
        }

        [Test]
        public void HandleEchoNestedCollection ()
        {
            var list0 = new List<string> { "jeb", "bob" };
            var list1 = new List<string> ();
            var list2 = new List<string> { "bill", "edzor" };
            var collection = new Dictionary<int, IList<string>> { { 0, list0 }, { 1, list1 }, { 2, list2 } };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()))
                .Returns ((IDictionary<int,IList<string>> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoNestedCollection", Arg (0, collection)));
            mock.Verify (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            CollectionAssert.AreEqual (collection, (IDictionary<int, IList<string>>)result.Value);
        }

        [Test]
        public void HandleEchoListOfObjects ()
        {
            var list = new List<TestService.TestClass> {
                new TestService.TestClass ("foo"),
                new TestService.TestClass ("bar")
            };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns ((IList<TestService.TestClass> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "EchoListOfObjects", Arg (0, list)));
            mock.Verify (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.IsNotNull (result.Value);
            CollectionAssert.AreEqual (list, (IList<TestService.TestClass>)result.Value);
        }

        /// <summary>
        /// Test calling a service method that takes an optional tuple as an argument
        /// </summary>
        [Test]
        public void HandleOptionalTupleNotSpecified ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.TupleDefault (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()))
                .Returns ((KRPC.Utils.Tuple<int,bool> x) => x);
            TestService.Service = mock.Object;
            var result = Run (Call ("TestService", "TupleDefault"));
            mock.Verify (x => x.TupleDefault (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()), Times.Once ());
            CheckResultNotEmpty (result);
            Assert.AreEqual (TestService.CreateTupleDefault.Create (), result.Value);
        }

        /// <summary>
        /// Test calling a service method that is not active in the current game mode
        /// </summary>
        [Test]
        public void ExecuteCallWrongGameMode ()
        {
            CallContext.SetGameScene (GameScene.TrackingStation);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Call ("TestService", "ProcedureNoArgsNoReturn")));
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Never ());
        }
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    [TestFixture]
    class TimeoutableStreamTests
    {
        [Test]
        public void TestDefaultTimeout()
        {
            bool okclient = false;
            bool okserver = false;
            bool pipeready = false;
            var pipename = "test.pipe." + RndCrypto.NextBytes(10).ToHex();
            var tc = new Thread(() =>
            {
                while (!pipeready) Thread.Sleep(50);
                Thread.Sleep(50);
                using (var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous))
                using (var binary = new BinaryStream(pipe))
                {
                    pipe.Connect();
                    Thread.Sleep(100);
                    binary.WriteString("TEST");
                    Thread.Sleep(500);
                    binary.WriteString("TEST");
                    Thread.Sleep(100);
                    binary.ReadString();
                    Thread.Sleep(500);
                    binary.ReadString();

                    Thread.Sleep(100);
                    okclient = true;
                }
            });
            tc.Start();

            var ts = new Thread(() =>
            {
                using (var pipe = new NamedPipeServerStream(pipename, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                using (var timeout = new TimeoutableStream(pipe))
                using (var binary = new BinaryStream(timeout))
                {
                    pipeready = true;
                    pipe.WaitForConnection();
                    binary.ReadString();
                    binary.ReadString();
                    binary.WriteString("TEST");
                    binary.WriteString("TEST");
                    Thread.Sleep(100);
                    okserver = true;
                }
            });
            ts.Start();

            tc.Join();
            ts.Join();

            Assert.IsTrue(okclient);
            Assert.IsTrue(okserver);
        }

        [Test]
        public void TestReadTimeout()
        {
            bool okclient = false;
            bool okserver = false;
            bool pipeready = false;
            var pipename = "test.pipe." + RndCrypto.NextBytes(10).ToHex();
            var tc = new Thread(() =>
            {
                while (!pipeready) Thread.Sleep(50);
                Thread.Sleep(50);
                using (var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous))
                using (var binary = new BinaryStream(pipe))
                {
                    pipe.Connect();
                    Thread.Sleep(100);
                    binary.WriteString("TEST");
                    Thread.Sleep(500);
                    try { binary.WriteString("TEST"); Assert.Fail(); } // can't succeed because the server timeout breaks the pipe.
                    catch (IOException) { }

                    Thread.Sleep(100);
                    okclient = true;
                }
            });
            tc.Start();

            var ts = new Thread(() =>
            {
                using (var pipe = new NamedPipeServerStream(pipename, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                using (var timeout = new TimeoutableStream(pipe))
                using (var binary = new BinaryStream(timeout))
                {
                    pipeready = true;
                    pipe.WaitForConnection();
                    timeout.ReadTimeout = 300;
                    binary.ReadString();
                    try { binary.ReadString(); Assert.Fail(); }
                    catch (TimeoutException) { }
                    Thread.Sleep(100);
                    okserver = true;
                }
            });
            ts.Start();

            tc.Join();
            ts.Join();

            Assert.IsTrue(okclient);
            Assert.IsTrue(okserver);
        }

        [Test]
        public void TestWriteTimeout()
        {
            bool okclient = false;
            bool okserver = false;
            bool pipeready = false;
            var pipename = "test.pipe." + RndCrypto.NextBytes(10).ToHex();
            var tc = new Thread(() =>
            {
                while (!pipeready) Thread.Sleep(50);
                Thread.Sleep(50);
                using (var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous))
                using (var binary = new BinaryStream(pipe))
                {
                    pipe.Connect();
                    Thread.Sleep(100);
                    binary.ReadString();
                    Thread.Sleep(500);
                    try { binary.ReadString(); Assert.Fail(); } // can't succeed because the server timeout breaks the pipe.
                    catch (IOException) { }

                    Thread.Sleep(100);
                    okclient = true;
                }
            });
            tc.Start();

            var ts = new Thread(() =>
            {
                using (var pipe = new NamedPipeServerStream(pipename, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                using (var timeout = new TimeoutableStream(pipe))
                using (var binary = new BinaryStream(timeout))
                {
                    pipeready = true;
                    pipe.WaitForConnection();
                    timeout.WriteTimeout = 300;
                    binary.WriteString("TEST");
                    try { binary.WriteString("TEST"); Assert.Fail(); }
                    catch (TimeoutException) { }
                    Thread.Sleep(100);
                    okserver = true;
                }
            });
            ts.Start();

            tc.Join();
            ts.Join();

            Assert.IsTrue(okclient);
            Assert.IsTrue(okserver);
        }
    }
}

using System;
using System.Threading.Tasks;
using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using NUnit.Framework;

namespace Test
{
    public class ProcessResponseProcessor_Tests
    {
        private AutoMoqer mocker;
        private ProcessResponseProcessor processor;
        private Func<Task> successCallback;
        private bool successCallbackCalled;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            processor = mocker.Create<ProcessResponseProcessor>();
            successCallbackCalled = false;
            successCallback = () =>
            {
                successCallbackCalled = true;
                return Task.CompletedTask;
                
            };
            
        }

        [Test]
        public async Task Should_Return_False_If_Response_Is_Null_As_Cancelled()
        {
            Assert.IsFalse(await processor.ProcessAsync(null, null, true, null, successCallback));
            Assert.IsFalse(successCallbackCalled);
        }

        [Test]
        public async Task Should_Throw_Exception_If_Non_Success_ExitCode_And_Throw_Error_True()
        {
            var executeResponse = new ExecuteResponse();
            executeResponse.ExitCode = 999;
            executeResponse.Output = "This will be exception message";
            var callbackExitCode = 0;
            Assert.Throws<Exception>(async() =>
            {
                await processor.ProcessAsync(executeResponse, exitCode =>
                {
                    callbackExitCode = exitCode;
                    return false;
                }, true, null, null);
            }, executeResponse.Output);
            Assert.AreEqual(executeResponse.ExitCode, callbackExitCode);
        }

        [Test]
        public async Task Should_Log_Response_Output_With_Error_Title_If_Non_Success_ExitCode_And_Throw_Error_False()
        {
            var executeResponse = new ExecuteResponse();
            executeResponse.ExitCode = 999;
            executeResponse.Output = "This will be logged";
            Assert.False(await processor.ProcessAsync(executeResponse, exitCode =>
                {
                    return false;
                }, false, "title", successCallback));

            Assert.IsFalse(successCallbackCalled);
            mocker.Verify<ILogger>(l => l.Log("title Error", "This will be logged"));
            
        }

        [Test]
        public async Task Should_Log_Response_Output_With_Title_If_Success_ExitCode_And_Call_Callback()
        {
            var executeResponse = new ExecuteResponse();
            executeResponse.ExitCode = 0;
            executeResponse.Output = "This will be logged";
            Assert.True(await processor.ProcessAsync(executeResponse, exitCode =>
            {
                return true;
            }, true, "title", successCallback));

            Assert.IsTrue(successCallbackCalled);
            mocker.Verify<ILogger>(l => l.Log("title", "This will be logged"));
        }
    }
}
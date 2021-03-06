﻿using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.CircuitBreaker;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Tests.Retry.CircuitBreaker
{
    [TestFixture]
    public class CircuitBreakerTests : FixtureBase
    {
        // TODO: Fix this test
        [Test]
        [Ignore("Temporary ignored, For some reason this test fails, even though worker count explicity set to 0, but worker count returns 1. " +
            "This needs to be fixed before merge")] 
        public async Task CircuitBreakerIntegrationTest()
        {
            var network = new InMemNetwork();

            var receiver = Using(new BuiltinHandlerActivator());
            var bus = Configure.With(receiver)
                  .Logging(l => l.Trace())
                  .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
                  .Options(o =>
                      {
                          o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(1, trackingPeriodInSeconds: 10));
                      }
                  )
                  .Start();

            receiver.Handle<string>(async (buss, context, message) =>
            {
                await Task.FromResult(0);
                throw new MyCustomException();
            });

            await bus.SendLocal("Uh oh, This is not gonna go well!");

            await Task.Delay(5000);


            var workerCount = bus.Advanced.Workers.Count;
            Assert.That(workerCount, Is.EqualTo(0), $"Expected worker count to be '0' but was {workerCount}");
        }

        class MyCustomException : Exception
        {

        }
    }
}
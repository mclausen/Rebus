﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMessageDeferral : FixtureBase
    {
        readonly InMemNetwork _network = new InMemNetwork(true);

        BuiltinHandlerActivator _server;
        BuiltinHandlerActivator _client;

        protected override void SetUp()
        {
            _network.Reset();

            _server = CreateBus("test.message.deferral", configurer =>
            {
                configurer.Timeouts(t => t.StoreInMemory());
            });

            _client = CreateBus("test.message.deferral.CLIENT", configurer =>
            {
                configurer.Routing(r =>
                {
                    r.TypeBased()
                        .Map<string>("test.message.deferral");
                });
            });
        }

        [Test]
        public async Task CanDeferMessage_ToSelf()
        {
            var messageReceived = new ManualResetEvent(false);
            var deliveryTime = DateTime.MaxValue;

            _server.Handle<string>(async s =>
            {
                deliveryTime = DateTime.UtcNow;
                messageReceived.Set();
            });

            var sendTime = DateTime.UtcNow;
            var delay = TimeSpan.FromSeconds(5);

            await _server.Bus.DeferLocal(delay, "hej med dig!");

            messageReceived.WaitOrDie(TimeSpan.FromSeconds(8));

            var timeToBeDelivered = deliveryTime - sendTime;

            Assert.That(timeToBeDelivered, Is.GreaterThanOrEqualTo(delay));
        }

        [Test]
        public async Task CanDeferMessage_ToAnotherDestination()
        {
            var messageReceived = new ManualResetEvent(false);
            var deliveryTime = DateTime.MaxValue;

            _server.Handle<string>(async s =>
            {
                deliveryTime = DateTime.UtcNow;
                messageReceived.Set();
            });

            var sendTime = DateTime.UtcNow;
            var delay = TimeSpan.FromSeconds(5);

            await _client.Bus.DeferLocal(delay, "hej med dig!");

            messageReceived.WaitOrDie(TimeSpan.FromSeconds(8));

            var timeToBeDelivered = deliveryTime - sendTime;

            Assert.That(timeToBeDelivered, Is.GreaterThanOrEqualTo(delay));
        }

        BuiltinHandlerActivator CreateBus(string queueName, Action<RebusConfigurer> additionalConfiguration = null)
        {
            var activator = new BuiltinHandlerActivator();

            Using(_server);

            var configurer = Configure.With(_server)
                .Transport(t => t.UseInMemoryTransport(_network, queueName));

            additionalConfiguration?.Invoke(configurer);

            configurer.Start();

            return activator;
        }
    }
}
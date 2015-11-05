using System;
using System.Threading.Tasks;
using Carrot.Configuration;
using Carrot.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Carrot
{
    public abstract class ConsumerBase : DefaultBasicConsumer, IDisposable
    {
        private readonly IConsumedMessageBuilder _builder;
        private readonly ConsumingConfiguration _configuration;

        protected internal ConsumerBase(IModel model,
                                        IConsumedMessageBuilder builder,
                                        ConsumingConfiguration configuration)
            : base(model)
        {
            _builder = builder;
            _configuration = configuration;
        }

        public override void HandleBasicDeliver(String consumerTag,
                                                UInt64 deliveryTag,
                                                Boolean redelivered,
                                                String exchange,
                                                String routingKey,
                                                IBasicProperties properties,
                                                Byte[] body)
        {
            base.HandleBasicDeliver(consumerTag,
                                    deliveryTag,
                                    redelivered,
                                    exchange,
                                    routingKey,
                                    properties,
                                    body);

            var args = new BasicDeliverEventArgs
                           {
                               ConsumerTag = consumerTag,
                               DeliveryTag = deliveryTag,
                               Redelivered = redelivered,
                               Exchange = exchange,
                               RoutingKey = routingKey,
                               BasicProperties = properties,
                               Body = body
                           };

            ConsumeInternalAsync(args);
        }

        public void Dispose()
        {
            if (Model != null)
                Model.Dispose();
        }

        protected internal Task<AggregateConsumingResult> ConsumeAsync(BasicDeliverEventArgs args)
        {
            return _builder.Build(args).ConsumeAsync(_configuration);
        }

        protected abstract Task<AggregateConsumingResult> ConsumeInternalAsync(BasicDeliverEventArgs args);
    }
}
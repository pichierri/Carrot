using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Carrot.Extensions;
using RabbitMQ.Client;

namespace Carrot
{
    public interface IConnectionBuilder
    {
        RabbitMQ.Client.IConnection CreateConnection(Uri endpointUri);
    }

    internal class ConnectionBuilder : IConnectionBuilder
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public ConnectionBuilder(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public RabbitMQ.Client.IConnection CreateConnection(Uri endpointUri)
        {
            if (endpointUri == null)
                throw new ArgumentNullException(nameof(endpointUri));

            var connectionFactory = BuildConnectionFactory(endpointUri);

            return connectionFactory.CreateConnection();
        }

        protected virtual ConnectionFactory BuildConnectionFactory(Uri endpointUri)
        {
            var factory = new ConnectionFactory
                              {
                                  Uri = endpointUri.ToString(),
                                  AutomaticRecoveryEnabled = true,
                                  TopologyRecoveryEnabled = true
                              };
            var assembly = typeof(Broker).Assembly;
            var properties = new Dictionary<String, Object>
                                 {
                                     { "client_api", ProductName(assembly) },
                                     { "client_version", assembly.GetName().Version.ToString() },
                                     { "net_version", Environment.Version.ToString() },
                                     { "hostname", Environment.MachineName },
                                     { "connected_on", _dateTimeProvider.UtcNow().ToString("R") },
                                     { "process_id", Process.GetCurrentProcess().Id.ToString() },
                                     { "process_name", Process.GetCurrentProcess().ProcessName }
                                 };
            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
                properties.Add("entry_assembly", entryAssembly.GetName().Name);

            factory.ClientProperties = properties;

            return factory;
        }

        private static String ProductName(Assembly assembly)
        {
            return assembly.GetCustomAttributes<AssemblyProductAttribute>()
                           .SingleOrDefault(new AssemblyProductAttribute("Carrot"))
                           .Product;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EasyNetQ
{
    internal static class ConnectionConfigurationExtensions
    {
        public static void SetDefaultProperties(this ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            if (
                configuration.AmqpConnectionString != null &&
                configuration.Hosts.All(h => h.Host != configuration.AmqpConnectionString.Host)
            )
            {
                if (configuration.Port == ConnectionConfiguration.DefaultPort)
                {
                    if (configuration.AmqpConnectionString.Port > 0)
                        configuration.Port = (ushort) configuration.AmqpConnectionString.Port;
                    else if (
                        configuration.AmqpConnectionString.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase)
                    )
                        configuration.Port = ConnectionConfiguration.DefaultAmqpsPort;
                }

                if (configuration.AmqpConnectionString.Segments.Length > 1)
                    configuration.VirtualHost = configuration.AmqpConnectionString.Segments.Last();

                configuration.Hosts.Add(new HostConfiguration {Host = configuration.AmqpConnectionString.Host});
            }

            if (configuration.Hosts.Count == 0)
                throw new EasyNetQException(
                    "Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");

            foreach (var hostConfiguration in configuration.Hosts)
                if (hostConfiguration.Port == 0)
                    hostConfiguration.Port = configuration.Port;

#if !NETFX
            var version = typeof(ConnectionConfigurationExtensions).GetTypeInfo().Assembly.GetName().Version.ToString();
#else
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
            var applicationNameAndPath = Environment.GetCommandLineArgs()[0];

            var applicationName = "unknown";
            var applicationPath = "unknown";
            if (!string.IsNullOrWhiteSpace(applicationNameAndPath))
                try
                {
                    // Will only throw an exception if the applicationName contains invalid characters, is empty, or too long
                    // Silently catch the exception, as we will just leave the application name and path to "unknown"
                    applicationName = Path.GetFileName(applicationNameAndPath);
                    applicationPath = Path.GetDirectoryName(applicationNameAndPath);
                }
                catch (ArgumentException)
                {
                }
                catch (PathTooLongException)
                {
                }

            var hostname = Environment.MachineName;

            var netVersion = Environment.Version.ToString();
            configuration.Product ??= applicationName;
            configuration.Platform ??= hostname;
            configuration.Name ??= applicationName;

            AddValueIfNotExists(configuration.ClientProperties, "client_api", "EasyNetQ");
            AddValueIfNotExists(configuration.ClientProperties, "product", configuration.Product);
            AddValueIfNotExists(configuration.ClientProperties, "platform", configuration.Platform);
            AddValueIfNotExists(configuration.ClientProperties, "net_version", netVersion);
            AddValueIfNotExists(configuration.ClientProperties, "version", version);
            AddValueIfNotExists(configuration.ClientProperties, "easynetq_version", version);
            AddValueIfNotExists(configuration.ClientProperties, "application", applicationName);
            AddValueIfNotExists(configuration.ClientProperties, "application_location", applicationPath);
            AddValueIfNotExists(configuration.ClientProperties, "machine_name", hostname);
            AddValueIfNotExists(configuration.ClientProperties, "timeout", configuration.Timeout.ToString());
            AddValueIfNotExists(
                configuration.ClientProperties, "publisher_confirms", configuration.PublisherConfirms.ToString()
            );
            AddValueIfNotExists(
                configuration.ClientProperties, "persistent_messages", configuration.PersistentMessages.ToString()
            );
        }

        private static void AddValueIfNotExists(IDictionary<string, object> clientProperties, string name, string value)
        {
            if (!clientProperties.ContainsKey(name))
                clientProperties.Add(name, value);
        }
    }
}

using System;
using System.Configuration;

namespace FluentMigrator.Runner.Initialization
{
    /// <summary>
    /// Locates connection strings by name in assembly's config file or machine.config
    /// If no connection matches it uses the specified connection string as valid connection
    /// </summary>
    public class ConnectionStringManager
    {
        private readonly IAnnouncer announcer;
        private readonly string assemblyLocation;
        private readonly INetConfigManager configManager;
        private readonly string configPath;
        private readonly string database;
        private readonly bool doNotAnnounceConnectionString;
        private string configFile;
        private string connection;
        private Func<string> machineNameProvider = () => Environment.MachineName;
        private bool notUsingConfig;

        public ConnectionStringManager(INetConfigManager configManager, IAnnouncer announcer, string connection, string configPath, string assemblyLocation, string database, bool doNotAnnounceConnectionString)
        {
            this.connection = connection;
            this.configPath = configPath;
            this.database = database;
            this.assemblyLocation = assemblyLocation;
            notUsingConfig = true;
            this.configManager = configManager;
            this.announcer = announcer;
            this.doNotAnnounceConnectionString = doNotAnnounceConnectionString;
        }

        public string ConnectionString { get; private set; }

        public Func<string> MachineNameProvider
        {
            get { return machineNameProvider; }
            set { machineNameProvider = value; }
        }

        public void LoadConnectionString()
        {
            if (notUsingConfig && !string.IsNullOrEmpty(configPath))
                LoadConnectionStringFromConfigurationFile(configManager.LoadFromFile(configPath));

            if (notUsingConfig && !String.IsNullOrEmpty(assemblyLocation))
            {
                string defaultConfigFile = assemblyLocation;

                LoadConnectionStringFromConfigurationFile(configManager.LoadFromFile(defaultConfigFile));
            }

            if (notUsingConfig)
                LoadConnectionStringFromConfigurationFile(configManager.LoadFromMachineConfiguration());

            if (notUsingConfig && !string.IsNullOrEmpty(connection))
                ConnectionString = connection;

            OutputResults();
        }

        private void LoadConnectionStringFromConfigurationFile(Configuration configurationFile)
        {
            var connections = configurationFile.ConnectionStrings.ConnectionStrings;

            if (connections == null || connections.Count <= 0)
                return;

            ConnectionStringSettings connectionString;

            if (string.IsNullOrEmpty(connection))
                connectionString = connections[MachineNameProvider()];
            else
                connectionString = connections[connection];

            ReadConnectionString(connectionString, configurationFile.FilePath);
        }

        private void ReadConnectionString(ConnectionStringSettings connectionSetting, string configurationFile)
        {
            if (connectionSetting == null) return;

            connection = connectionSetting.Name;
            ConnectionString = connectionSetting.ConnectionString;
            configFile = configurationFile;
            notUsingConfig = false;
        }

        private void OutputResults()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new UndeterminableConnectionException("Unable to resolve any connectionstring using parameters \"/connection\" and \"/configPath\"");
            if (notUsingConfig)
            {
                if (doNotAnnounceConnectionString)
                {
                    announcer.Say(string.Format("Using Database {0}", database));
                }
                else
                {
                    announcer.Say(string.Format("Using Database {0} and Connection String {1}", database, ConnectionString));
                }
            }
            else{ announcer.Say(string.Format("Using Connection {0} from Configuration file {1}", connection, configFile));
            }
        }
    }
}
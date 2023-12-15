// Copyright (c) 2023 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using MTConnect.Agents;
using MTConnect.Configurations;
using MTConnect.Haas;

namespace MTConnect.Modules
{
    public class Module : MTConnectAgentModule
    {
        public const string ConfigurationTypeId = "haas-ethernet";
        private const string ModuleId = "Haas Ethernet Adapter";

        private readonly ModuleConfiguration _configuration;
        private readonly HaasEthernetDataSource _dataSource;


        public Module(IMTConnectAgentBroker mtconnectAgent, object configuration) : base(mtconnectAgent)
        {
            Id = ModuleId;
            _configuration = AgentApplicationConfiguration.GetConfiguration<ModuleConfiguration>(configuration);
            _dataSource = new HaasEthernetDataSource(_configuration);
            _dataSource.ObservationAdded += DataSourceObservationAdded;

            // Needs to be changed
            var dataSourceConfiguration = new AdapterApplicationConfiguration();
            dataSourceConfiguration.ReadInterval = 1000;
            _dataSource.Configuration = dataSourceConfiguration;
        }

        private void DataSourceObservationAdded(object? sender, Input.IObservationInput observation)
        {
            Agent.AddObservation(_configuration.DeviceKey, observation);
        }

        protected override void OnStartAfterLoad()
        {
            _dataSource.Start();
        }

        protected override void OnStop()
        {
            _dataSource.Stop();
        }
    }
}
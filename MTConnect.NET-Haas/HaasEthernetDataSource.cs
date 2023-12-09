// Copyright (c) 2023 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace MTConnect.Haas
{
    public class HaasEthernetDataSource : HaasDataSource
    {
        private readonly Ethernet _ethernet;

        public HaasEthernetDataSource(HaasEthernetConfiguration configuration)
        {
            _ethernet = new Ethernet(configuration.Server, configuration.Port);
        }


        protected override void OnStart()
        {
            if (_ethernet != null) _ethernet.Connect();
        }

        protected override void OnStop()
        {
            if (_ethernet != null) _ethernet.Close();
        }

        protected override string SendCommand(string command) => _ethernet.SendCommand(command);
    }
}

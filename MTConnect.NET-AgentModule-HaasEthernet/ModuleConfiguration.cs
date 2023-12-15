// Copyright (c) 2023 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using MTConnect.Haas;

namespace MTConnect.Configurations
{
    public class ModuleConfiguration : HaasEthernetConfiguration
    {
        public string DeviceKey { get; set; }


        public ModuleConfiguration()
        {
            Server = "localhost";
            Port = 5051;
        }
    }
}
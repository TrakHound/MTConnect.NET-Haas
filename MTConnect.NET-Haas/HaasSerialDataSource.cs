// Copyright (c) 2023 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace MTConnect.Haas
{
    public class HaasSerialDataSource : HaasDataSource
    {
        private readonly HaasSerialConfiguration _configuration;

        public HaasSerialDataSource(HaasSerialConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override string SendCommand(string command)
        {
            var responses = Serial.SendCommand(command, _configuration.COMPort);
            if (responses != null && responses.Length > 1)
            {
                return responses[1];
            }

            return null;
        }
    }
}

// Copyright (c) 2023 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using MTConnect.Input;
using MTConnect.Logging;
using MTConnect.Observations;
using MTConnect.Observations.Events;
using System.Text.RegularExpressions;

namespace MTConnect.Haas
{
    public abstract class HaasDataSource : MTConnectDataSource
    {
        // Conditions
        public string mZeroRetKey = "zero_ret";
        public string mSystemKey = "system";

        // Events
        public string mMessageKey = "message";
        public string mEstopKey = "estop";
        public string mExecutionKey = "execution";
        public string mPartCountKey = "partCount";
        public string mProgramKey = "program";
        public string mModeKey = "mode";
        public string mAvailKey = "avail";

        // Samples
        public string mXactKey = "x_act";
        public string mYactKey = "y_act";
        public string mZactKey = "z_act";
        public string mSpindleSpeedKey = "speed";


        protected override void OnRead()
        {
            ProcessQ100();
            ProcessQ104();
            ProcessQ500();
            ProcessQ600();
        }


        protected virtual string SendCommand(string command) => null;


        #region "Standard Variables"

        private void ProcessQ100()
        {
            var response = SendCommand("?Q100");
            if (!string.IsNullOrEmpty(response))
            {
                Log(MTConnectLogLevel.Debug, "Q100 : " + response);

                ProcessAvailability(response);
            }
        }

        private void ProcessQ104()
        {
            string response = SendCommand("?Q104");
            if (!string.IsNullOrEmpty(response))
            {
                Log(MTConnectLogLevel.Debug, "Q104 : " + response);

                ProcessControllerMode(response);
                ProcessZeroReturn(response);
            }
        }

        private void ProcessQ500()
        {
            string response = SendCommand("?Q500");
            if (!string.IsNullOrEmpty(response))
            {
                Log(MTConnectLogLevel.Debug, "Q500 : " + response);

                ProcessExecution(response);
                ProcessEmergencyStop(response);
                ProcessProgramName(response);
                ProcessPartCount(response);
            }
        }


        private void ProcessAvailability(string response)
        {
            var pattern = "^>SERIAL NUMBER, (.*)$";
            var match = new Regex(pattern).Match(response);
            if (match.Success) AddObservation(mAvailKey, Availability.AVAILABLE);
            else AddObservation(mAvailKey, Availability.UNAVAILABLE);
        }

        private void ProcessExecution(string response)
        {
            var pattern = "^>PROGRAM, .*, (.*), PARTS, [0-9]*$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                if (val == "IDLE") AddObservation(mExecutionKey, Execution.READY);
                else if (val == "FEED HOLD") AddObservation(mExecutionKey, Execution.INTERRUPTED);
                else if (val == "ALARM ON") AddObservation(mExecutionKey, Execution.STOPPED);
            }

            pattern = "^>STATUS (.*)$";
            match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                if (val == "BUSY") AddObservation(mExecutionKey, Execution.ACTIVE);
            }
        }

        private void ProcessEmergencyStop(string response)
        {
            var pattern = "^>PROGRAM, .*, (.*), PARTS, [0-9]*$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                if (val == "ALARM ON")
                {
                    AddObservation(mEstopKey, EmergencyStop.TRIGGERED);

                    var fault = new ConditionFaultStateObservationInput();
                    fault.DataItemKey = mSystemKey;
                    fault.Level = ConditionLevel.FAULT;
                    fault.Message = "Alarm on indicator";
                    AddObservation(fault);
                }
                else
                {
                    AddObservation(mEstopKey, EmergencyStop.ARMED);
                    AddObservation(new ConditionFaultStateObservationInput(mSystemKey, ConditionLevel.NORMAL));
                }
            }
            else
            {
                AddObservation(mEstopKey, EmergencyStop.ARMED);
                AddObservation(new ConditionFaultStateObservationInput(mSystemKey, ConditionLevel.NORMAL));
            }
        }

        private void ProcessProgramName(string response)
        {
            var pattern = "^>PROGRAM, (.*), .*, PARTS, [0-9]*$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                if (val == "MDI") AddObservation(mProgramKey, "");
                else AddObservation(mProgramKey, val);
            }
        }

        private void ProcessPartCount(string response)
        {
            var pattern = "^>PROGRAM, .*, .*, PARTS, ([0-9]*)$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                AddObservation(mPartCountKey, match.Groups[1].ToString());
            }
        }

        private void ProcessControllerMode(string response)
        {
            var pattern = "^>MODE, (.*)$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                switch (val)
                {
                    case "(MDI)": AddObservation(mModeKey, ControllerMode.MANUAL_DATA_INPUT); break;
                    case "(JOG)": AddObservation(mModeKey, ControllerMode.MANUAL); break;
                    case "(ZERO RET)": AddObservation(mModeKey, ControllerMode.MANUAL); break;
                    default: AddObservation(mModeKey, ControllerMode.AUTOMATIC); break;
                }
            }
        }

        private void ProcessZeroReturn(string response)
        {
            var pattern = "^>MODE, (.*)$";
            var match = new Regex(pattern).Match(response);
            if (match.Success && match.Groups.Count > 1)
            {
                var val = match.Groups[1].ToString();
                switch (val)
                {
                    case "(ZERO RET)":

                        var fault = new ConditionFaultStateObservationInput();
                        fault.DataItemKey = mZeroRetKey;
                        fault.Level = ConditionLevel.FAULT;
                        fault.Message = "NO ZERO X";
                        AddObservation(fault);
                        break;

                    default: 
                        AddObservation(new ConditionFaultStateObservationInput(mZeroRetKey, ConditionLevel.NORMAL));
                        break;
                }
            }
        }

        #endregion

        #region "Custom Variables (Q600)"

        private void ProcessQ600()
        {
            ProcessAxisActualPositions();
            ProcessSpindle();
        }

        private string GetVariable(int variable)
        {
            string response = SendCommand("?Q600 " + variable);
            if (!string.IsNullOrEmpty(response))
            {
                Log(MTConnectLogLevel.Debug, $"Q600 : {variable} : {response}");

                var pattern = "^>MACRO, (.*)$";
                var match = new Regex(pattern).Match(response);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].ToString();
                }
            }

            return null;
        }

        #region "Axis Positions"

        private void ProcessAxisActualPositions()
        {
            ProcessAxisActualPosition_X();
            ProcessAxisActualPosition_Y();
            ProcessAxisActualPosition_Z();
        }

        private void ProcessAxisActualPosition_X()
        {
            string s = GetVariable(5041);
            if (s != null)
            {
                AddObservation(mXactKey, s);
            }
        }

        private void ProcessAxisActualPosition_Y()
        {
            string s = GetVariable(5042);
            if (s != null)
            {
                AddObservation(mYactKey, s);
            }
        }

        private void ProcessAxisActualPosition_Z()
        {
            string s = GetVariable(5043);
            if (s != null)
            {
                AddObservation(mZactKey, s);
            }
        }

        #endregion

        #region "Spindle"

        private void ProcessSpindle()
        {
            ProcessSpindle_Speed();
        }

        private void ProcessSpindle_Speed()
        {
            string s = GetVariable(3027);
            if (s != null)
            {
                AddObservation(mSpindleSpeedKey, s);
            }
        }

        #endregion

        #endregion

    }
}

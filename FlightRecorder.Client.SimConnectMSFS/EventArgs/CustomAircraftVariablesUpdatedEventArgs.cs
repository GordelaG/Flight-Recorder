using System;

namespace FlightRecorder.Client.SimConnectMSFS;

public class CustomAircraftVariablesUpdatedEventArgs : EventArgs
{
    public CustomAircraftVariablesStruct Variables { get; }

    public CustomAircraftVariablesUpdatedEventArgs(CustomAircraftVariablesStruct variables)
    {
        Variables = variables;
    }
}

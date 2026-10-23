using System.Collections.Generic;

namespace FlightRecorder.Client.SimConnectMSFS;

public class AircraftVariable
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "number";
    public string Description { get; set; } = string.Empty;
    public bool Interpolate { get; set; } = true;

    public AircraftVariable() { }

    public AircraftVariable(string name, string unit = "number", string description = "", bool interpolate = true)
    {
        Name = name;
        Unit = unit;
        Description = string.IsNullOrEmpty(description) ? name : description;
        Interpolate = interpolate;
    }
}

public class AircraftProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public List<string> MatchTitles { get; set; } = new();
    public List<AircraftVariable> Variables { get; set; } = new();

    public AircraftProfile() { }

    public AircraftProfile(string id, string name, string vendor, IEnumerable<string> matchTitles, IEnumerable<AircraftVariable> variables)
    {
        Id = id;
        Name = name;
        Vendor = vendor;
        MatchTitles.AddRange(matchTitles);
        Variables.AddRange(variables);
    }
}

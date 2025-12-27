namespace Core.Entities;

public class Actuator
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string Name { get; set; }
    public ActuatorType Type { get; set; }
    public IEnumerable<ActuatorState>? SupportedStates { get; set; }
    public IEnumerable<ActuatorCommand>? SupportedCommands { get; set; }
    public Dictionary<ActuatorState, object?>? States { get; set; }

    public Device? Device { get; set; }

    public Actuator(Guid id, Guid deviceId, string name, ActuatorType type,
        IEnumerable<ActuatorState>? supportedStates, IEnumerable<ActuatorCommand>? supportedCommands)
    {
        Id = id;
        DeviceId = deviceId;
        Name = name;
        Type = type;
        SupportedStates = supportedStates;
        SupportedCommands = supportedCommands;

        States = supportedStates?.ToDictionary(state => state, state => (object?)null);
    }

    public bool TryUpdateState(ActuatorState state, string? value)
    {
        if (States is null || !States.ContainsKey(state))
            return false;

        States[state] = value;
        return true;
    }
}


public enum ActuatorType
{
    Light,
    Fan
}

public enum ActuatorState
{
    Power,
    Speed
}

public enum ActuatorCommand
{
    TurnOn,
    TurnOff
}

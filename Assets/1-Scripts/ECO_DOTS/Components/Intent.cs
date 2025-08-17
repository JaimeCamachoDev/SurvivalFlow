using Unity.Entities;

/// <summary>
/// Estado actual deseado del herbívoro.
/// </summary>
public struct Intent : IComponentData
{
    /// 0=Idle,1=Forage,2=Eat,3=Flee,4=Roam,5=Mate,6=Rest
    public byte State;
}

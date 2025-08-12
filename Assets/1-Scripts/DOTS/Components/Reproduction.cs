using Unity.Entities;

/// <summary>
/// Datos de reproducción de un herbívoro.
/// </summary>
public struct Reproduction : IComponentData
{
    /// Nivel de hambre necesario para poder reproducirse.
    public float Threshold;
    /// Radio de búsqueda activa de pareja.
    public float SeekRadius;
    /// Distancia mínima para considerar que están apareados.
    public float MatingDistance;
    /// Tiempo de enfriamiento entre reproducciones.
    public float Cooldown;
    /// Temporizador restante hasta poder reproducirse de nuevo.
    public float Timer;
    /// Número mínimo de crías por reproducción.
    public int MinOffspring;
    /// Número máximo de crías por reproducción.
    public int MaxOffspring;
}

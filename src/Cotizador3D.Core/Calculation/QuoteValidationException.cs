namespace Cotizador3D.Core.Calculation;

/// <summary>
/// Error de validacion de una entrada del usuario. El mensaje esta en espanol
/// claro y se muestra tal cual en la UI (decision B3/B10: aviso, no traceback).
/// </summary>
public sealed class QuoteValidationException : Exception
{
    public QuoteValidationException(string mensaje)
        : base(mensaje)
    {
    }

    public QuoteValidationException(string mensaje, string? campo)
        : base(mensaje)
    {
        Campo = campo;
    }

    /// <summary>Nombre logico del campo que fallo, si se conoce (para resaltarlo en la UI).</summary>
    public string? Campo { get; }
}

namespace GestaoDePedidos.Common.Validation;

public static class QuantidadeValidator
{
    private const int MaxCasasDecimaisFracionado = 3;

    public static bool IsValid(decimal valor, bool permiteVendaFracionada)
    {
        var casasDecimais = ContarCasasDecimais(valor);

        return permiteVendaFracionada
            ? casasDecimais <= MaxCasasDecimaisFracionado
            : casasDecimais == 0;
    }

    private static int ContarCasasDecimais(decimal valor)
    {
        var bits = decimal.GetBits(valor);
        return (bits[3] >> 16) & 0x7F;
    }
}

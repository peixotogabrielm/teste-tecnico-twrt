namespace GestaoDePedidos.Common.Validation;

public static class ValorMonetarioValidator
{
    private const int CasasDecimais = 2;

    public static bool IsValid(decimal valor)
    {
        return ContarCasasDecimais(RemoverZerosAExtra(valor)) <= CasasDecimais;
    }

    public static decimal Arredondar(decimal valor)
    {
        return Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);
    }

    private static int ContarCasasDecimais(decimal valor)
    {
        var bits = decimal.GetBits(valor);
        return (bits[3] >> 16) & 0x7F;
    }

    private static decimal RemoverZerosAExtra(decimal valor)
    {
        return valor / 1.000000000000000000000000000000000M;
    }
}

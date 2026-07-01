namespace GestaoDePedidos.Common.Validation;

public static class DocumentoValidator
{
    public static bool IsValid(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return false;
        }

        var digitos = new string(documento.Where(char.IsDigit).ToArray());

        return digitos.Length switch
        {
            11 => IsCpfValido(digitos),
            14 => IsCnpjValido(digitos),
            _ => false
        };
    }

    private static bool IsCpfValido(string cpf)
    {
        if (TodosDigitosIguais(cpf))
        {
            return false;
        }

        var primeiroDigito = CalcularDigitoVerificador(cpf[..9], [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var segundoDigito = CalcularDigitoVerificador(cpf[..9] + primeiroDigito, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cpf[9] - '0' == primeiroDigito && cpf[10] - '0' == segundoDigito;
    }

    private static bool IsCnpjValido(string cnpj)
    {
        if (TodosDigitosIguais(cnpj))
        {
            return false;
        }

        var primeiroDigito = CalcularDigitoVerificador(cnpj[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var segundoDigito = CalcularDigitoVerificador(cnpj[..12] + primeiroDigito, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cnpj[12] - '0' == primeiroDigito && cnpj[13] - '0' == segundoDigito;
    }

    private static int CalcularDigitoVerificador(string digitos, int[] pesos)
    {
        var soma = digitos.Select((c, i) => (c - '0') * pesos[i]).Sum();
        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }

    private static bool TodosDigitosIguais(string digitos) => digitos.Distinct().Count() == 1;
}

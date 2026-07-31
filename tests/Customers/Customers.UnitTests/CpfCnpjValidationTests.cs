using Customers.Domain.ValueObjects;
using FluentAssertions;

namespace Customers.UnitTests;

/// <summary>
/// Regra de validação de CPF/CNPJ (dígito verificador) preservada do GearFlow, exercitada pela
/// superfície pública dos VOs. É a mesma regra que a Lambda de CPF reusa — vale cobrir os casos-limite.
/// </summary>
public sealed class CpfCnpjValidationTests
{
    // ---------- CPF ----------

    [Theory]
    [InlineData("52998224725")]        // só dígitos
    [InlineData("529.982.247-25")]     // formatado (pontos/traço são ignorados)
    public void Cpf_valid_is_accepted_and_normalized_to_digits(string input)
    {
        var result = Cpf.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("11111111111")]        // todos os dígitos iguais
    [InlineData("00000000000")]
    [InlineData("52998224726")]        // dígito verificador errado
    [InlineData("123")]                // tamanho inválido
    [InlineData("")]                   // vazio
    public void Cpf_invalid_is_rejected(string input)
    {
        var result = Cpf.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cpf.Invalid");
    }

    // ---------- CNPJ ----------

    [Theory]
    [InlineData("79385786000162")]         // só dígitos
    [InlineData("79.385.786/0001-62")]     // formatado
    public void Cnpj_valid_is_accepted_and_normalized_to_digits(string input)
    {
        var result = Cnpj.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("79385786000162");
    }

    [Theory]
    [InlineData("79385786000163")]     // dígito verificador errado
    [InlineData("123")]                // tamanho inválido
    [InlineData("")]                   // vazio
    public void Cnpj_invalid_is_rejected(string input)
    {
        var result = Cnpj.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cnpj.Invalid");
    }
}

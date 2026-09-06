using FluentAssertions;
using UrlShortener.Domain.Services;
using Xunit;

namespace UrlShortener.Tests.Domain;

public sealed class CryptographicCodeGeneratorTests
{
    private readonly CryptographicCodeGenerator _sut = new();

    [Fact]
    public void GenerateCode_ShouldReturnStringOfExactLengthSeven()
    {
        // Act
        string code = _sut.GenerateCode();

        // Assert
        code.Should().HaveLength(7);
    }

    [Fact]
    public void GenerateCode_ShouldContainOnlyBase62Characters()
    {
        // Act
        string code = _sut.GenerateCode();

        // Assert
        // MatchRegex на строке валидирует соответствие алфавиту Base62 
        // без приведения строки к IEnumerable<char>, избегая лишних аллокаций в тестах.
        code.Should().MatchRegex("^[0-9A-Za-z]+$");
    }

    [Fact]
    public void GenerateCode_ShouldGenerateUniqueCodesOnMultipleCalls()
    {
        // Arrange
        const int sampleSize = 1000;
        var generatedCodes = new HashSet<string>(sampleSize);

        // Act
        for (int i = 0; i < sampleSize; i++)
        {
            generatedCodes.Add(_sut.GenerateCode());
        }

        // Assert
        generatedCodes.Should().HaveCount(sampleSize, "коллизии на выборке в 1000 элементов недопустимы");
    }
}
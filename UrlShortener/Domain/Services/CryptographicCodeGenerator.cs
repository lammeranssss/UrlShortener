using System.Security.Cryptography;

namespace UrlShortener.Domain.Services;

public sealed class CryptographicCodeGenerator : IShortCodeGenerator
{
    private static readonly char[] Base62Chars = 
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    private const int CodeLength = 7;

    public string GenerateCode()
    {
        // stackalloc размещает буфер в стеке потока (Zero-Allocations).
        // Это исключает давление на Heap и паузы сборщика мусора (GC) при массовой генерации.
        Span<char> result = stackalloc char[CodeLength];

        for (int i = 0; i < CodeLength; i++)
        {
            // GetInt32(max) устраняет Modulo Bias, в отличие от оператора %,
            // обеспечивая криптографически равномерное распределение вероятности выборки символов.
            int index = RandomNumberGenerator.GetInt32(Base62Chars.Length);
            result[i] = Base62Chars[index];
        }

        return new string(result);
    }
}

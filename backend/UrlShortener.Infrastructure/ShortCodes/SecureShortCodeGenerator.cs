using System.Security.Cryptography;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure.ShortCodes;

public sealed class SecureShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private readonly int length;

    public SecureShortCodeGenerator(int length = 8)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Code length must be greater than zero.");
        }

        this.length = length;
    }

    public string Generate()
    {
        Span<char> code = stackalloc char[length];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}

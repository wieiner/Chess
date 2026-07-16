using System.Security.Cryptography;
using System.Text;

namespace RubikState;

public static class RubikStateHasher
{
    private const string Scheme = "U=white,R=red,F=green,D=yellow,L=orange,B=blue";

    public static string Calculate(RubikStateDocument document)
    {
        var builder = new StringBuilder(96 + document.Faces.Flatten().Length * 2);
        builder.Append("rubik.state|1|")
            .Append(document.Size)
            .Append('|')
            .Append(Scheme)
            .Append('|');

        var first = true;
        foreach (var value in document.Faces.Flatten())
        {
            if (!first)
                builder.Append(',');
            builder.Append(value);
            first = false;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}

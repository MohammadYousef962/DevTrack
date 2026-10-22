namespace DevTrack.Application.Common;

public static class AvatarRules
{
    public const int MaxBytes = 256 * 1024;

    // Looks at the file's opening bytes instead of trusting the file name or the Content-Type the client sent.
    public static string? DetectContentType(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (data.StartsWith(pngSignature))
            return "image/png";

        // WebP is a RIFF container: "RIFF", four size bytes, then "WEBP".
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }
}
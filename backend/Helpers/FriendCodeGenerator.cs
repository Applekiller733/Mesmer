using System.Security.Cryptography;

namespace SongAppApi.Helpers
{
    /// generates short, opaque, unique friend codes for accounts.
    ///
    /// format: 6 characters from an unambiguous alphabet
    /// displayed as XXX-YYY for readability but stored as a 6-char string
    /// total keyspace: 31^6 ≈ 887 million combinations — plenty of room
    /// for collision-resistant random generation even with millions of users.
    ///
    public static class FriendCodeGenerator
    {
        private const string ALPHABET = "ABCDEFGHJKMNPQRSTWXYZ23456789";
        private const int CODE_LENGTH = 6;

        public static string Generate()
        {
            //todo add collision detection 
            var bytes = new byte[CODE_LENGTH];
            RandomNumberGenerator.Fill(bytes);

            var chars = new char[CODE_LENGTH];
            for (int i = 0; i < CODE_LENGTH; i++)
            {
                chars[i] = ALPHABET[bytes[i] % ALPHABET.Length];
            }
            return new string(chars);
        }

        public static string ToDisplay(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length != CODE_LENGTH)
                return raw;
            return $"{raw[..3]}-{raw[3..]}";
        }

        public static string? TryNormalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var cleaned = input.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            if (cleaned.Length != CODE_LENGTH) return null;
            foreach (var c in cleaned)
            {
                if (!ALPHABET.Contains(c)) return null;
            }
            return cleaned;
        }
    }
}
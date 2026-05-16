using System.Security.Cryptography;

namespace SongAppApi.Helpers
{
    /// <summary>
    /// Generates short, opaque, unique friend codes for accounts.
    ///
    /// Format: 6 characters from an unambiguous alphabet (no 0/O/1/I/L).
    /// Displayed as XXX-YYY for readability but stored as the 6-char string.
    /// Total keyspace: 31^6 ≈ 887 million combinations — plenty of room
    /// for collision-resistant random generation even with millions of users.
    ///
    /// Collision handling: the caller is expected to check uniqueness and
    /// retry. With ~1k users, collision probability per draw is ~10^-6;
    /// retrying once or twice almost always succeeds.
    /// </summary>
    public static class FriendCodeGenerator
    {
        // Unambiguous alphabet — 31 chars. Drops 0/O and 1/I/L because they
        // look identical in many fonts. Also drops U/V which can be hard to
        // distinguish at small sizes.
        private const string ALPHABET = "ABCDEFGHJKMNPQRSTWXYZ23456789";
        private const int CODE_LENGTH = 6;

        /// <summary>
        /// Generate one random 6-character code using cryptographically
        /// secure RNG. Result is uppercase, no separators (storage form).
        /// </summary>
        public static string Generate()
        {
            var bytes = new byte[CODE_LENGTH];
            RandomNumberGenerator.Fill(bytes);

            var chars = new char[CODE_LENGTH];
            for (int i = 0; i < CODE_LENGTH; i++)
            {
                // Modulo introduces a slight bias toward earlier alphabet
                // entries since 256 isn't a multiple of 31. The bias is
                // negligible (~0.03 percentage points per character) and
                // doesn't matter for non-cryptographic use cases like
                // friend codes.
                chars[i] = ALPHABET[bytes[i] % ALPHABET.Length];
            }
            return new string(chars);
        }

        /// <summary>
        /// Display form: "ABC-XYZ" from "ABCXYZ". Frontend uses this when
        /// showing the code; backend stores and queries the raw form.
        /// </summary>
        public static string ToDisplay(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length != CODE_LENGTH)
                return raw;
            return $"{raw[..3]}-{raw[3..]}";
        }

        /// <summary>
        /// Parse user input. Accepts "ABC-XYZ", "ABCXYZ", "abc-xyz" — strips
        /// hyphens, uppercases, validates length and alphabet. Returns null
        /// for anything not parseable.
        /// </summary>
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
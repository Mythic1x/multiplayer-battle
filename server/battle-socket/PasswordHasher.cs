using System.Text;
using Geralt;
public class PasswordHasher {
    private static readonly int iterations = 3;
    private static readonly int memorySize = 33554432;
    public static string HashPassword(string password) {
        var pass = Encoding.UTF8.GetBytes(password);
        var hash = new Byte[Argon2id.MaxHashSize];
        try {
            Argon2id.ComputeHash(hash, pass, iterations, memorySize);
        } catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
        var hashedPassword = Encoding.UTF8.GetString(hash).TrimEnd('\0');
        return hashedPassword;
    }
    public static bool VerifyPassword(string hash, string password) {
        return Argon2id.VerifyHash(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(password));
    }
}

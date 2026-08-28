using System.Security.Cryptography;

namespace ToggleAvailability.Server.Services;

public class AdminAuthenticationService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly int _iterations;

    private readonly byte[] _salt;
    private readonly byte[] _hash;


    public AdminAuthenticationService(
        IConfiguration configuration)
    {
        string? storedHash =
            configuration[
                "AdminAuthentication:PasswordHash"];

        if (string.IsNullOrWhiteSpace(storedHash))
        {
            throw new InvalidOperationException(
                "AdminAuthentication:PasswordHash is not configured.");
        }


        // --------------------------------------------------
        // Expected format:
        //
        // iterations$salt$hash
        // --------------------------------------------------

        string[] parts =
            storedHash.Split(
                '$',
                StringSplitOptions.RemoveEmptyEntries);


        if (parts.Length != 3 ||
            !int.TryParse(
                parts[0],
                out int iterations))
        {
            throw new InvalidOperationException(
                "The configured admin password hash is invalid.");
        }


        if (iterations <= 0)
        {
            throw new InvalidOperationException(
                "The configured admin password hash contains " +
                "an invalid iteration count.");
        }


        _iterations =
            iterations;


        // --------------------------------------------------
        // Decode salt and password hash.
        // --------------------------------------------------

        try
        {
            _salt =
                Convert.FromBase64String(
                    parts[1]);

            _hash =
                Convert.FromBase64String(
                    parts[2]);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "The configured admin password hash is invalid.");
        }


        // --------------------------------------------------
        // Validate sizes.
        // --------------------------------------------------

        if (_salt.Length != SaltSize ||
            _hash.Length != HashSize)
        {
            throw new InvalidOperationException(
                "The configured admin password hash is invalid.");
        }
    }


    // ==================================================
    // Verify Passcode
    // ==================================================

    /// <summary>
    /// Determines whether the supplied passcode matches
    /// the configured administrator passcode.
    /// </summary>
    public bool VerifyPasscode(
        string passcode)
    {
        if (string.IsNullOrEmpty(passcode))
        {
            return false;
        }


        byte[] suppliedHash;

        try
        {
            suppliedHash =
                Rfc2898DeriveBytes.Pbkdf2(
                    passcode,
                    _salt,
                    _iterations,
                    HashAlgorithmName.SHA256,
                    HashSize);

            Console.WriteLine(
                $"Passcode length: {passcode.Length}");

            Console.WriteLine(
                $"Configured iterations: {_iterations}");

            Console.WriteLine(
                $"Configured salt: {Convert.ToBase64String(_salt)}");

            Console.WriteLine(
                $"Configured hash: {Convert.ToBase64String(_hash)}");

            Console.WriteLine(
                $"Calculated hash: {Convert.ToBase64String(suppliedHash)}");


        }
        catch
        {
            return false;
        }

        // --------------------------------------------------
        // Fixed-time comparison prevents timing attacks.
        // --------------------------------------------------

        return CryptographicOperations.FixedTimeEquals(
            suppliedHash,
            _hash);
    }
}
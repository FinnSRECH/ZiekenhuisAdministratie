using System.Security.Cryptography;

namespace Hospital.Admin.Services;

public class PasswordService
{
	private const int SaltSize = 16;
	private const int HashSize = 32;
	private const int Iterations = 100_000;

	public string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
			password,
			salt,
			Iterations,
			HashAlgorithmName.SHA256,
			HashSize);

		return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
	}

	public bool VerifyPassword(
		string password,
		string storedPasswordHash)
	{
		if (string.IsNullOrWhiteSpace(storedPasswordHash))
		{
			return false;
		}

		var parts = storedPasswordHash.Split('.');

		if (parts.Length != 2)
		{
			return false;
		}

		try
		{
			byte[] salt = Convert.FromBase64String(parts[0]);
			byte[] storedHash = Convert.FromBase64String(parts[1]);

			byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
				password,
				salt,
				Iterations,
				HashAlgorithmName.SHA256,
				HashSize);

			return CryptographicOperations.FixedTimeEquals(
				storedHash,
				enteredHash);
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
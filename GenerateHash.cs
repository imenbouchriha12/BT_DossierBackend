// Petit programme pour générer le hash BCrypt correct
// Compilez et exécutez avec: dotnet script GenerateHash.cs

using System;

class Program
{
    static void Main()
    {
        var password = "Admin@123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash BCrypt: {hash}");
        Console.WriteLine($"Verification: {BCrypt.Net.BCrypt.Verify(password, hash)}");
        
        // SQL pour mettre à jour
        Console.WriteLine();
        Console.WriteLine("SQL UPDATE:");
        Console.WriteLine($"UPDATE dbo.AppReg_Users SET PasswordHash = '{hash}' WHERE Matricule = 'ADMIN001';");
    }
}

using System;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    /// <summary>
    /// Kimlik doğrulama işlemlerini yöneten yardımcı sınıf
    /// Şifreler BCrypt ile hashlenmiş olarak saklanır
    /// </summary>
    public static class AuthHelper
    {
        /// <summary>
        /// Şu an oturum açmış kullanıcı
        /// </summary>
        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Kullanıcı giriş işlemi - BCrypt ile şifre doğrulama
        /// </summary>
        public static bool Login(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Kullanıcı adı boş bırakılamaz!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Şifre boş bırakılamaz!";
                return false;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Önce kullanıcıyı username ile bul
                    string query = @"SELECT id, full_name, username, password, role, email, phone, is_active, created_at, updated_at 
                                     FROM users 
                                     WHERE username = @username AND is_active = 1";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader.GetString("password");

                                // BCrypt ile şifre doğrulama
                                // Ayrıca eski (plaintext) şifreleri de destekle (geçiş dönemi)
                                bool passwordValid = false;
                                try
                                {
                                    passwordValid = BCrypt.Net.BCrypt.Verify(password, storedPassword);
                                }
                                catch
                                {
                                    // Hash formatında değilse düz metin karşılaştır (eski kayıtlar)
                                    passwordValid = (password == storedPassword);
                                }

                                if (passwordValid)
                                {
                                    CurrentUser = new User
                                    {
                                        Id = reader.GetInt32("id"),
                                        FullName = reader.GetString("full_name"),
                                        Username = reader.GetString("username"),
                                        Password = storedPassword,
                                        Role = reader.GetString("role"),
                                        Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                        Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                                        IsActive = reader.GetBoolean("is_active"),
                                        CreatedAt = reader.GetDateTime("created_at"),
                                        UpdatedAt = reader.GetDateTime("updated_at")
                                    };

                                    // Eğer şifre eski formatta (plaintext) ise, arka planda hashle
                                    if (!storedPassword.StartsWith("$2"))
                                    {
                                        reader.Close();
                                        MigratePassword(CurrentUser.Id, password);
                                    }

                                    return true;
                                }
                                else
                                {
                                    errorMessage = "Kullanıcı adı veya şifre hatalı!";
                                    return false;
                                }
                            }
                            else
                            {
                                errorMessage = "Kullanıcı adı veya şifre hatalı!";
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Veritabanı hatası: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Şifreyi hashler (BCrypt)
        /// </summary>
        public static string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 11);
        }

        /// <summary>
        /// Şifre doğrulama (BCrypt)
        /// </summary>
        public static bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
            catch
            {
                // Hash formatında değilse düz metin karşılaştır (eski kayıtlar)
                return plainPassword == hashedPassword;
            }
        }

        /// <summary>
        /// Eski plaintext şifreyi BCrypt hash'e dönüştürür (otomatik migration)
        /// </summary>
        private static void MigratePassword(int userId, string plainPassword)
        {
            try
            {
                string hashed = HashPassword(plainPassword);
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new MySqlCommand("UPDATE users SET password = @pwd WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@pwd", hashed);
                        cmd.Parameters.AddWithValue("@id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Migration hatası sessizce yutulabilir, giriş başarılı olmuştur zaten
            }
        }

        /// <summary>
        /// Kullanıcı çıkış işlemi
        /// </summary>
        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}

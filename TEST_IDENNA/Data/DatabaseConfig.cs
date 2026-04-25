using Microsoft.Data.Sqlite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Data
{
/*internal class DatabaseConfig
    {
        private const string ConnectionString = "Data Source=idenna.db";

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Ninos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombres TEXT,
                Apellidos TEXT,
                FechaNacimiento TEXT,
                SituacionActual TEXT,
                UbicacionArchivoFisico TEXT
            );";
                command.ExecuteNonQuery();
            }
        }
        public static void GuardarNino(string nombre)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Ninos (Nombres) VALUES ($nombre)";
                command.Parameters.AddWithValue("$nombre", nombre);
                command.ExecuteNonQuery();
            }
        }

        public static List<Nino> ObtenerTodos()
        {
            var lista = new List<Nino>();
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Nombres FROM Ninos";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Nino { Id = reader.GetInt32(0), Nombres = reader.GetString(1) });
                    }
                }
            }
            return lista;
        }
    }
                    
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Registrar/Inicializar el proveedor nativo de SQLite
            SQLitePCL.Batteries.Init();

            DatabaseConfig.InitializeDatabase(); // Crea la DB al iniciar

        }
    }*/
}

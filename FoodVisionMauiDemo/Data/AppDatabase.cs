using FoodVisionMauiDemo.Models;
using SQLite;

namespace FoodVisionMauiDemo.Data
{
    public class AppDatabase
    {
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private SQLiteAsyncConnection? _database;

        public string DatabasePath { get; } = Path.Combine(FileSystem.Current.AppDataDirectory, "nutrilenskg.db3");

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_database != null)
                return _database;

            await _initializationLock.WaitAsync();
            try
            {
                if (_database != null)
                    return _database;

                SQLitePCL.Batteries_V2.Init();

                _database = new SQLiteAsyncConnection(
                    DatabasePath,
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);

                await _database.CreateTableAsync<ScanRecord>();
                await _database.CreateTableAsync<PredictionResult>();
                await _database.CreateTableAsync<FoodNodeSnapshot>();

                return _database;
            }
            finally
            {
                _initializationLock.Release();
            }
        }
    }
}

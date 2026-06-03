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
                await EnsureScanRecordColumnsAsync(_database);
                await EnsureFoodNodeSnapshotColumnsAsync(_database);

                return _database;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private static async Task EnsureScanRecordColumnsAsync(SQLiteAsyncConnection database)
        {
            await TryAddColumnAsync(database, "ALTER TABLE ScanRecord ADD COLUMN VoiceNotePath TEXT DEFAULT ''");
            await TryAddColumnAsync(database, "ALTER TABLE ScanRecord ADD COLUMN VoiceNoteSizeBytes INTEGER DEFAULT 0");
        }

        private static async Task EnsureFoodNodeSnapshotColumnsAsync(SQLiteAsyncConnection database)
        {
            await TryAddColumnAsync(database, "ALTER TABLE FoodNodeSnapshot ADD COLUMN RiskScoresJson TEXT DEFAULT '{}'");
        }

        private static async Task TryAddColumnAsync(SQLiteAsyncConnection database, string sql)
        {
            try
            {
                await database.ExecuteAsync(sql);
            }
            catch
            {
                // Column already exists or migration is not needed.
            }
        }
    }
}

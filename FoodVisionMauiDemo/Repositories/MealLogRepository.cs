using FoodVisionMauiDemo.Data;
using FoodVisionMauiDemo.Models;

namespace FoodVisionMauiDemo.Repositories
{
    public class MealLogRepository
    {
        private readonly AppDatabase _database;

        public MealLogRepository()
            : this(new AppDatabase())
        {
        }

        public MealLogRepository(AppDatabase database)
        {
            _database = database;
        }

        public string DatabasePath => _database.DatabasePath;

        public async Task<int> SaveMealAsync(
            ScanRecord record,
            IEnumerable<PredictionResult> predictions,
            FoodNodeSnapshot snapshot)
        {
            var connection = await _database.GetConnectionAsync();
            var predictionList = predictions.ToList();

            await connection.RunInTransactionAsync(transaction =>
            {
                transaction.Insert(record);

                foreach (var prediction in predictionList)
                {
                    prediction.ScanRecordId = record.Id;
                    transaction.Insert(prediction);
                }

                snapshot.ScanRecordId = record.Id;
                transaction.Insert(snapshot);
            });

            return record.Id;
        }

        public async Task<IReadOnlyList<MealLogItem>> GetTodayMealsAsync()
        {
            var localStart = DateTime.Today;
            var localEnd = localStart.AddDays(1);
            var utcStart = localStart.ToUniversalTime();
            var utcEnd = localEnd.ToUniversalTime();

            var connection = await _database.GetConnectionAsync();
            var records = await connection.Table<ScanRecord>()
                .Where(record => record.CreatedAtUtc >= utcStart && record.CreatedAtUtc < utcEnd)
                .OrderByDescending(record => record.CreatedAtUtc)
                .ToListAsync();

            return await BuildMealLogItemsAsync(records);
        }

        public async Task<IReadOnlyList<MealLogItem>> GetRecentMealsAsync(int limit = 30)
        {
            var utcStart = DateTime.Today.AddDays(-6).ToUniversalTime();
            var connection = await _database.GetConnectionAsync();
            var records = await connection.Table<ScanRecord>()
                .Where(record => record.CreatedAtUtc >= utcStart)
                .OrderByDescending(record => record.CreatedAtUtc)
                .Take(limit)
                .ToListAsync();

            return await BuildMealLogItemsAsync(records);
        }

        public async Task<IReadOnlyList<RiskMealSnapshot>> GetRiskMealsSinceAsync(DateTime utcStart)
        {
            var connection = await _database.GetConnectionAsync();
            var records = await connection.Table<ScanRecord>()
                .Where(record => record.CreatedAtUtc >= utcStart)
                .OrderByDescending(record => record.CreatedAtUtc)
                .ToListAsync();

            var meals = new List<RiskMealSnapshot>();

            foreach (var record in records)
            {
                var snapshot = await connection.Table<FoodNodeSnapshot>()
                    .Where(item => item.ScanRecordId == record.Id)
                    .FirstOrDefaultAsync();

                meals.Add(new RiskMealSnapshot
                {
                    ScanRecordId = record.Id,
                    CreatedAtUtc = record.CreatedAtUtc,
                    FoodKey = snapshot?.FoodKey ?? record.ConfirmedLabel,
                    FoodName = !string.IsNullOrWhiteSpace(record.FoodName) ? record.FoodName : record.ConfirmedLabel,
                    Portion = record.Portion,
                    Tags = snapshot?.Tags ?? new List<string>(),
                    RiskScores = snapshot?.RiskScores ?? new Dictionary<string, double>(),
                    Allergens = snapshot?.Allergens ?? new List<string>(),
                    Alternatives = snapshot?.Alternatives ?? new List<string>()
                });
            }

            return meals;
        }

        public async Task DeleteMealAsync(int scanRecordId)
        {
            var connection = await _database.GetConnectionAsync();

            await connection.RunInTransactionAsync(transaction =>
            {
                transaction.Execute("DELETE FROM PredictionResult WHERE ScanRecordId = ?", scanRecordId);
                transaction.Execute("DELETE FROM FoodNodeSnapshot WHERE ScanRecordId = ?", scanRecordId);
                transaction.Execute("DELETE FROM ScanRecord WHERE Id = ?", scanRecordId);
            });
        }

        public async Task UpdateMealAsync(int scanRecordId, string mealType, string portion, string notes)
        {
            var connection = await _database.GetConnectionAsync();
            var record = await connection.Table<ScanRecord>()
                .Where(item => item.Id == scanRecordId)
                .FirstOrDefaultAsync();

            if (record == null)
                throw new InvalidOperationException("Meal record not found.");

            record.MealType = mealType;
            record.Portion = portion;
            record.Notes = notes?.Trim() ?? string.Empty;
            await connection.UpdateAsync(record);
        }

        private async Task<IReadOnlyList<MealLogItem>> BuildMealLogItemsAsync(IReadOnlyList<ScanRecord> records)
        {
            var connection = await _database.GetConnectionAsync();
            var items = new List<MealLogItem>();

            foreach (var record in records)
            {
                var snapshot = await connection.Table<FoodNodeSnapshot>()
                    .Where(item => item.ScanRecordId == record.Id)
                    .FirstOrDefaultAsync();

                items.Add(new MealLogItem
                {
                    Id = record.Id,
                    FoodName = !string.IsNullOrWhiteSpace(record.FoodName) ? record.FoodName : record.ConfirmedLabel,
                    ConfirmedLabel = record.ConfirmedLabel,
                    MealType = record.MealType,
                    Portion = record.Portion,
                    ImagePath = record.ImagePath,
                    Notes = record.Notes,
                    VoiceNotePath = record.VoiceNotePath,
                    VoiceNoteSizeBytes = record.VoiceNoteSizeBytes,
                    CreatedAtUtc = record.CreatedAtUtc,
                    RiskTags = snapshot?.Tags ?? new List<string>()
                });
                items[^1].BeginEdit();
            }

            return items;
        }
    }
}

using SQLite;
using AutoLog.Models;
using System.IO;
using System.Threading.Tasks;

namespace AutoLog.Services;

public class LocalDatabase
{
    private SQLiteAsyncConnection _database;

    async Task Init()
    {
        if (_database is not null)
            return;

        // Створюємо файл бази даних у локальній папці пристрою
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AutoLogDB.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        // Створюємо таблиці
        await _database.CreateTableAsync<Vehicle>();
        await _database.CreateTableAsync<FuelRecord>();
        await _database.CreateTableAsync<User>();
    }

    // Приклад методу: Отримати всі авто
    public async Task<List<Vehicle>> GetVehiclesAsync()
    {
        await Init();
        return await _database.Table<Vehicle>().ToListAsync();
    }

    public async Task<int> DeleteVehicleAsync(Vehicle vehicle)
{
    await Init();
    // Спочатку видаляємо всі заправки, пов'язані з цим авто, щоб не було "осиротілих" даних
    var records = await GetFuelRecordsAsync(vehicle.Id);
    foreach (var record in records)
    {
        await _database.DeleteAsync(record);
    }
    
    // Тепер видаляємо саме авто
    return await _database.DeleteAsync(vehicle);
}

    public async Task<int> SaveVehicleAsync(Vehicle vehicle)
    {
    await Init();
    // Якщо Id != 0, значить оновлюємо існуюче, інакше - створюємо нове
    if (vehicle.Id != 0)
        return await _database.UpdateAsync(vehicle);
    else
        return await _database.InsertAsync(vehicle);
    }

    public async Task<int> SaveFuelRecordAsync(FuelRecord record)
{
    await Init();
    if (record.Id != 0)
        return await _database.UpdateAsync(record);
    else
        return await _database.InsertAsync(record);
}

public async Task<List<FuelRecord>> GetFuelRecordsAsync(int vehicleId)
{
    await Init();
    // Повертаємо лише ті записи, які належать конкретному авто
    return await _database.Table<FuelRecord>().Where(x => x.VehicleId == vehicleId).ToListAsync();
}

public async Task<int> DeleteFuelRecordAsync(FuelRecord record)
{
    await Init();
    return await _database.DeleteAsync(record);
}

public async Task<User?> GetUserByEmailAsync(string email)
{
    await Init();
    var lower = email.Trim().ToLower();
    return await _database.Table<User>().Where(u => u.Email.ToLower() == lower).FirstOrDefaultAsync();
}

public async Task<int> RegisterUserAsync(User user)
{
    await Init();
    return await _database.InsertAsync(user);
}

public async Task<List<FuelRecord>> GetAllFuelRecordsAsync()
{
    await Init();
    return await _database.Table<FuelRecord>().ToListAsync();
}
}
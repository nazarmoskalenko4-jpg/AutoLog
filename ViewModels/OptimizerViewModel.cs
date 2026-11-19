using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Services;
using AutoLog.Models;

namespace AutoLog.ViewModels;

public class OptimizerViewModel : INotifyPropertyChanged
{
    private readonly OptimizationService _optimizationService;

    // Властивості з дефолтними значеннями для зручного тестування
    public double FuelConsumption { get; set; } = 8.0;
    public double RefillVolume { get; set; } = 40;
    public double CurrentPrice { get; set; } = 55.00;
    public double TargetPrice { get; set; } = 51.50;
    public double Distance { get; set; } = 15;

    private string _resultText = "Введіть дані та натисніть розрахувати";
    public string ResultText 
    { 
        get => _resultText; 
        set { _resultText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultText))); } 
    }

    private Color _resultColor = Colors.Gray;
    public Color ResultColor 
    { 
        get => _resultColor; 
        set { _resultColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultColor))); } 
    }

    public ICommand CalculateCommand { get; }
    public event PropertyChangedEventHandler PropertyChanged;

    public OptimizerViewModel(OptimizationService optimizationService)
    {
        _optimizationService = optimizationService;
        CalculateCommand = new Command(Calculate);
    }

    private void Calculate()
    {
        // Створюємо "віртуальне" авто для передачі в сервіс
        var tempVehicle = new Vehicle { FuelConsumption = this.FuelConsumption };
        
        double benefit = _optimizationService.CalculateNetBenefit(
            tempVehicle, RefillVolume, CurrentPrice, TargetPrice, Distance);

        if (benefit > 0)
        {
            ResultText = $"Їхати вигідно! Чиста економія: {benefit} грн";
            ResultColor = Colors.Green;
        }
        else if (benefit < 0)
        {
            ResultText = $"Невигідно! Ви втратите {-benefit} грн на дорозі";
            ResultColor = Colors.Red;
        }
        else
        {
            ResultText = "Вийде в нуль. Немає сенсу витрачати час.";
            ResultColor = Colors.Orange;
        }
    }
}
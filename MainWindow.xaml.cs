using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopWidget.Services;

namespace DesktopWidget;

public partial class MainWindow : Window
{
    private readonly SystemMonitor monitor;
    private readonly AudioService audio;
    private readonly CommandService commands;
    private readonly DispatcherTimer timer;
    private bool updatingVolume;
    private bool closing;

    public MainWindow()
    {
        InitializeComponent();

        monitor = new SystemMonitor();
        audio = new AudioService();
        commands = new CommandService();

        updatingVolume = true;
        try
        {
            var initialVolume = audio.Volume;
            VolumeSlider.Value = initialVolume;
            VolumeText.Text = $"{initialVolume:F0}%";
            MuteButton.Content = audio.IsMuted ? "Unmute" : "Mute";
        }
        finally
        {
            updatingVolume = false;
        }

        Opacity = 0;
        var fadeIn = new DoubleAnimation(
            0, 0.96, TimeSpan.FromMilliseconds(300))
        {
            FillBehavior = FillBehavior.Stop
        };
        fadeIn.Completed += (_, _) =>
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = OpacitySlider.Value;
        };
        BeginAnimation(OpacityProperty, fadeIn);

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => UpdateSystemInfo();
        timer.Start();

        UpdateSystemInfo();

        Closed += (_, _) =>
        {
            closing = true;
            timer.Stop();
            monitor.Dispose();
            audio.Dispose();
        };
    }

    private void UpdateSystemInfo()
    {
        try
        {
            var cpu = monitor.GetCpuUsage();
            var availableMb = monitor.GetAvailableMemoryMb();

            CpuText.Text = $"CPU: {cpu:F1}%";
            CpuBar.Value = cpu;
            MemoryText.Text = $"Available RAM: {availableMb:N0} MB";
            ProcessText.Text = monitor.GetTopProcesses(5);
        }
        catch (InvalidOperationException ex)
        {
            ShowMonitorError(ex);
        }
        catch (Win32Exception ex)
        {
            ShowMonitorError(ex);
        }
    }

    private void ShowMonitorError(Exception exception)
    {
        CpuText.Text = "System monitor unavailable";
        CommandOutput.Text = $"System monitor error: {exception.Message}";
    }

    private void VolumeSlider_Changed(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (audio is null || updatingVolume)
        {
            return;
        }

        try
        {
            audio.Volume = (float)e.NewValue;
            VolumeText.Text = $"{e.NewValue:F0}%";
        }
        catch (COMException ex)
        {
            CommandOutput.Text = $"Audio error: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            CommandOutput.Text = $"Audio error: {ex.Message}";
        }
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            audio.ToggleMute();
            MuteButton.Content = audio.IsMuted ? "Unmute" : "Mute";
        }
        catch (COMException ex)
        {
            CommandOutput.Text = $"Audio error: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            CommandOutput.Text = $"Audio error: {ex.Message}";
        }
    }

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        CommandOutput.Text = "Executing...";
        CommandInput.IsEnabled = false;

        try
        {
            CommandOutput.Text = await commands.ExecuteAsync(CommandInput.Text);
        }
        catch (Win32Exception ex)
        {
            CommandOutput.Text = $"Command error: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            CommandOutput.Text = $"Command error: {ex.Message}";
        }
        catch (System.IO.IOException ex)
        {
            CommandOutput.Text = $"Command error: {ex.Message}";
        }
        finally
        {
            if (!closing)
            {
                CommandInput.IsEnabled = true;
            }
        }
    }

    private void OpacitySlider_Changed(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!closing)
        {
            Opacity = e.NewValue;
        }
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed
            && e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.Diagnostics;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace PcAudio;

public partial class MainWindow
{
    private readonly Dictionary<string, DeviceInformation> devices = new(StringComparer.OrdinalIgnoreCase);
    private DeviceWatcher? deviceWatcher;
    private bool refreshing;
    private bool updatingDeviceList;

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        StartDeviceWatcher();
        StartWatchingAudioOutput();
    }

    private void RefreshClicked(object sender, RoutedEventArgs e) => StartDeviceWatcher();

    private void StartDeviceWatcher()
    {
        StopDeviceWatcher();
        devices.Clear();
        refreshing = true;
        SetStatus(ConnectionStatus.Disconnected, "");
        SearchNotice.Visibility = Visibility.Collapsed;
        ListMessage.Text = "Procurando celulares compatíveis…";
        UpdateControls();

        try
        {
            // The native selector enumerates paired A2DP source interfaces.
            deviceWatcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
            deviceWatcher.Added += DeviceAdded;
            deviceWatcher.Updated += DeviceUpdated;
            deviceWatcher.Removed += DeviceRemoved;
            deviceWatcher.EnumerationCompleted += DeviceEnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcherStopped;
            deviceWatcher.Start();
        }
        catch (Exception error)
        {
            ReportDeviceWatcherFailure("Iniciar busca de celulares", error);
        }
    }

    private void StopDeviceWatcher()
    {
        var watcher = deviceWatcher;
        deviceWatcher = null;
        if (watcher is null)
        {
            return;
        }

        watcher.Added -= DeviceAdded;
        watcher.Updated -= DeviceUpdated;
        watcher.Removed -= DeviceRemoved;
        watcher.EnumerationCompleted -= DeviceEnumerationCompleted;
        watcher.Stopped -= DeviceWatcherStopped;

        if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
        {
            try
            {
                watcher.Stop();
            }
            catch (Exception error)
            {
                Trace.TraceWarning("Falha ao parar a busca Bluetooth ({0:X8}).", error.HResult);
            }
        }
    }

    private void DeviceAdded(DeviceWatcher watcher, DeviceInformation device) =>
        DispatchDeviceEvent(watcher, () =>
        {
            devices[device.Id] = device;
            if (!refreshing)
            {
                UpdateDeviceList();
            }
        });

    private void DeviceUpdated(DeviceWatcher watcher, DeviceInformationUpdate change) =>
        DispatchDeviceEvent(watcher, () =>
        {
            if (devices.TryGetValue(change.Id, out var device))
            {
                var previousName = device.Name;
                device.Update(change);
                if (!refreshing && previousName != device.Name)
                {
                    UpdateDeviceList();
                }
            }
        });

    private void DeviceRemoved(DeviceWatcher watcher, DeviceInformationUpdate change) =>
        DispatchDeviceEvent(watcher, () =>
        {
            if (!devices.Remove(change.Id) || refreshing)
            {
                return;
            }

            if (string.Equals(DeviceList.SelectedValue as string, change.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (connectionAttempt is { } pending)
                {
                    pending.Cancel();
                }
                else if (connection is not null)
                {
                    ReleaseConnection();
                    SetStatus(ConnectionStatus.Disconnected, "O celular saiu da lista de dispositivos disponíveis.");
                }
            }

            UpdateDeviceList();
            _ = CheckBluetoothWhenListIsEmptyAsync(watcher);
        });

    private void DeviceEnumerationCompleted(DeviceWatcher watcher, object args) =>
        DispatchDeviceEvent(watcher, () =>
        {
            refreshing = false;
            UpdateDeviceList();
            _ = CheckBluetoothWhenListIsEmptyAsync(watcher);
        });

    private void DeviceWatcherStopped(DeviceWatcher watcher, object args) =>
        DispatchDeviceEvent(watcher, () => ReportDeviceWatcherFailure("Acompanhar celulares emparelhados"));

    private void DispatchDeviceEvent(DeviceWatcher watcher, Action handleEvent)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            if (!closed && ReferenceEquals(deviceWatcher, watcher))
            {
                handleEvent();
            }
        });
    }

    private void UpdateDeviceList()
    {
        var selectedId = DeviceList.SelectedValue;
        updatingDeviceList = true;
        try
        {
            DeviceList.ItemsSource = devices.Values.OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            DeviceList.SelectedValue = selectedId;
        }
        finally
        {
            updatingDeviceList = false;
        }

        ListMessage.Text = devices.Count == 0
            ? "Nenhum celular compatível encontrado. Emparelhe o celular nas configurações do Windows."
            : "";
        UpdateControls();
    }

    private async Task CheckBluetoothWhenListIsEmptyAsync(DeviceWatcher watcher)
    {
        if (devices.Count != 0)
        {
            return;
        }

        try
        {
            var reason = await GetBluetoothUnavailableReasonAsync(windowLifetime.Token);
            if (!closed && ReferenceEquals(deviceWatcher, watcher) && devices.Count == 0 && reason is not null)
            {
                ListMessage.Text = reason;
                RecordDiagnostic("Verificar Bluetooth", reason);
            }
        }
        catch (Exception error)
        {
            Trace.TraceInformation("Não foi possível verificar o estado do Bluetooth ({0:X8}).", error.HResult);
        }
    }

    private void ReportDeviceWatcherFailure(string stage, Exception? error = null)
    {
        if (refreshing)
        {
            devices.Clear();
            UpdateDeviceList();
        }

        StopDeviceWatcher();
        refreshing = false;
        // After enumeration, the visible list and the device cache are already in sync.
        ListMessage.Text = "";
        const string message = "A busca foi interrompida. Use Atualizar para tentar de novo.";
        SearchNotice.Text = WithErrorCode(message, error);
        RecordDiagnostic(stage, message, error);
        SearchNotice.Visibility = Visibility.Visible;
        if (connection is null && connectionAttempt is null)
        {
            SetStatus(ConnectionStatus.Error, "");
        }
        UpdateControls();
    }
}

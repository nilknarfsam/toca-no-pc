using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Media.Audio;

namespace PcAudio;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource windowLifetime = new();
    private AudioPlaybackConnection? connection;
    private CancellationTokenSource? connectionAttempt;
    private bool refreshing;
    private bool closed;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async void RefreshClicked(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        refreshing = true;
        UpdateControls();
        var selectedId = DeviceList.SelectedValue;
        ListMessage.Text = "Procurando celulares compatíveis…";
        SetStatus(ConnectionStatus.Disconnected, "");

        try
        {
            var token = windowLifetime.Token;
            // The native selector enumerates paired A2DP source interfaces.
            var devices = await DeviceInformation.FindAllAsync(AudioPlaybackConnection.GetDeviceSelector())
                .AsTask(token);
            var unavailableReason = await GetBluetoothUnavailableReasonAsync(token);
            token.ThrowIfCancellationRequested();

            var pairedDevices = devices.OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            DeviceList.ItemsSource = unavailableReason is null ? pairedDevices : [];
            DeviceList.SelectedValue = selectedId;
            ListMessage.Text = unavailableReason ?? (pairedDevices.Length == 0
                ? "Nenhum celular compatível encontrado. Emparelhe o celular e clique em Atualizar."
                : "");
            if (unavailableReason is not null)
            {
                SetStatus(ConnectionStatus.Error, "Confira o Bluetooth nas configurações do Windows.");
            }
        }
        catch (Exception error) when (!closed)
        {
            DeviceList.ItemsSource = null;
            ListMessage.Text = "Não foi possível listar os celulares.";
            SetStatus(ConnectionStatus.Error, WithErrorCode("Confira o Bluetooth e clique em Atualizar.", error));
        }
        catch (Exception) when (closed)
        {
            // An outstanding WinRT operation may complete while the window is closing.
        }
        finally
        {
            refreshing = false;
            if (!closed)
            {
                UpdateControls();
            }
        }
    }

    private async void ConnectClicked(object sender, RoutedEventArgs e)
    {
        if (DeviceList.SelectedItem is not DeviceInformation device)
        {
            return;
        }

        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(windowLifetime.Token);
        connectionAttempt = attempt;
        var token = attempt.Token;
        SetStatus(ConnectionStatus.Connecting, "Abrindo a conexão de áudio com o celular…");
        UpdateControls();

        var status = ConnectionStatus.Error;
        string detail;
        try
        {
            var unavailableReason = await GetBluetoothUnavailableReasonAsync(token);
            token.ThrowIfCancellationRequested();
            if (unavailableReason is not null)
            {
                detail = unavailableReason;
            }
            else if ((connection = AudioPlaybackConnection.TryCreateFromId(device.Id)) is not { } playback)
            {
                detail = "Celular indisponível. Confira se está próximo e atualize a lista.";
            }
            else
            {
                playback.StateChanged += PlaybackStateChanged;
                await playback.StartAsync().AsTask(token);
                token.ThrowIfCancellationRequested();
                var result = await playback.OpenAsync().AsTask(token);
                token.ThrowIfCancellationRequested();

                detail = result.Status switch
                {
                    AudioPlaybackConnectionOpenResultStatus.Success =>
                        "O celular encerrou a conexão durante a abertura. Tente novamente.",
                    AudioPlaybackConnectionOpenResultStatus.RequestTimedOut =>
                        "O celular demorou a responder. Mantenha-o próximo e tente novamente.",
                    AudioPlaybackConnectionOpenResultStatus.DeniedBySystem =>
                        "O Windows recusou a conexão. Confira o Bluetooth e o emparelhamento.",
                    _ => WithErrorCode("Não foi possível abrir a conexão de áudio.", result.ExtendedError)
                };

                if (result.Status == AudioPlaybackConnectionOpenResultStatus.Success &&
                    playback.State == AudioPlaybackConnectionState.Opened)
                {
                    status = ConnectionStatus.Connected;
                    detail = "Reproduza áudio no celular e selecione o PC como saída de mídia, se necessário.";
                }
            }
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
            status = ConnectionStatus.Disconnected;
            detail = "A tentativa de conexão foi cancelada.";
        }
        catch (Exception error)
        {
            detail = WithErrorCode("Falha na conexão. Confira o Bluetooth e o emparelhamento.", error);
        }
        finally
        {
            connectionAttempt = null;
            if (status != ConnectionStatus.Connected)
            {
                ReleaseConnection();
            }
        }

        if (!closed)
        {
            SetStatus(status, detail);
            UpdateControls();
        }
    }

    private void DisconnectClicked(object sender, RoutedEventArgs e)
    {
        if (connectionAttempt is { } pending)
        {
            pending.Cancel();
            SetStatus(ConnectionStatus.Connecting, "Cancelando a tentativa de conexão…");
            UpdateControls();
            return;
        }

        ReleaseConnection();
        SetStatus(ConnectionStatus.Disconnected, "Conexão encerrada.");
        UpdateControls();
    }

    private void PlaybackStateChanged(AudioPlaybackConnection sender, object args)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            if (closed || !ReferenceEquals(sender, connection) || connectionAttempt is not null)
            {
                return;
            }

            try
            {
                if (sender.State == AudioPlaybackConnectionState.Closed)
                {
                    ReleaseConnection();
                    SetStatus(ConnectionStatus.Disconnected,
                        "A conexão foi encerrada. Confira o Bluetooth nos dois aparelhos.");
                    UpdateControls();
                }
            }
            catch (Exception error)
            {
                ReleaseConnection();
                SetStatus(ConnectionStatus.Error, WithErrorCode("Não foi possível consultar a conexão.", error));
                UpdateControls();
            }
        });
    }

    private void ReleaseConnection()
    {
        var playback = connection;
        connection = null;
        if (playback is null)
        {
            return;
        }

        try
        {
            playback.StateChanged -= PlaybackStateChanged;
        }
        catch (Exception error)
        {
            Trace.TraceWarning("Falha ao remover o evento Bluetooth ({0:X8}).", error.HResult);
        }

        try
        {
            playback.Dispose();
        }
        catch (Exception error)
        {
            Trace.TraceWarning("Falha ao liberar a conexão Bluetooth ({0:X8}).", error.HResult);
        }
    }

    private static async Task<string?> GetBluetoothUnavailableReasonAsync(CancellationToken token)
    {
        try
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync().AsTask(token);
            if (adapter is null)
            {
                return "Nenhum adaptador Bluetooth disponível.";
            }

            if (!adapter.IsClassicSupported)
            {
                return "O adaptador não oferece Bluetooth clássico, necessário para A2DP.";
            }

            var radio = await adapter.GetRadioAsync().AsTask(token);
            return radio?.State switch
            {
                RadioState.Off => "O Bluetooth está desligado.",
                RadioState.Disabled => "O adaptador Bluetooth está desabilitado.",
                _ => null
            };
        }
        catch (UnauthorizedAccessException)
        {
            // The radio check is advisory. Windows can still allow the audio connection.
            return null;
        }
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        closed = true;
        windowLifetime.Cancel();
        ReleaseConnection();
        windowLifetime.Dispose();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedName.Text = (DeviceList.SelectedItem as DeviceInformation)?.Name ?? "Nenhum celular selecionado";
        if (!refreshing && connection is null && connectionAttempt is null)
        {
            SetStatus(ConnectionStatus.Disconnected, "");
        }
        UpdateControls();
    }

    private void BluetoothSettingsClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true })?.Dispose();
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            MessageBox.Show(this, WithErrorCode("Abra Configurações > Bluetooth no Windows.", error),
                "Configurações de Bluetooth", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateControls()
    {
        var canSelect = !refreshing && connectionAttempt is null && connection is null;
        DeviceList.IsEnabled = canSelect;
        RefreshButton.IsEnabled = canSelect;
        ConnectButton.IsEnabled = canSelect && DeviceList.SelectedItem is not null;
        DisconnectButton.IsEnabled = connectionAttempt is { } pending
            ? !pending.IsCancellationRequested
            : connection is not null;
    }

    private void SetStatus(ConnectionStatus status, string detail)
    {
        (StatusText.Text, StatusText.Foreground) = status switch
        {
            ConnectionStatus.Connecting => ("Conectando", Brushes.SaddleBrown),
            ConnectionStatus.Connected => ("Conectado", Brushes.DarkGreen),
            ConnectionStatus.Error => ("Erro", Brushes.Maroon),
            _ => ("Desconectado", (Brush)FindResource("MutedText"))
        };
        StatusDetail.Text = detail;
    }

    private static string WithErrorCode(string message, Exception? error) =>
        error is null ? message : $"{message} (0x{error.HResult:X8})";

    private enum ConnectionStatus { Disconnected, Connecting, Connected, Error }
}

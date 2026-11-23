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
    private string? lastDiagnostic;
    private bool closed;

    public MainWindow()
    {
        InitializeComponent();
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
        var stage = "Verificar Bluetooth";
        Exception? failure = null;
        string? resultStatus = null;
        string detail;
        try
        {
            var unavailableReason = await GetBluetoothUnavailableReasonAsync(token);
            token.ThrowIfCancellationRequested();
            stage = unavailableReason is null ? "Criar conexão de áudio" : stage;
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
                stage = "Preparar conexão de áudio";
                playback.StateChanged += PlaybackStateChanged;
                await playback.StartAsync().AsTask(token);
                token.ThrowIfCancellationRequested();
                stage = "Abrir conexão de áudio";
                var result = await playback.OpenAsync().AsTask(token);
                token.ThrowIfCancellationRequested();
                resultStatus = result.Status.ToString();
                if (result.Status != AudioPlaybackConnectionOpenResultStatus.Success)
                {
                    failure = result.ExtendedError;
                }

                detail = result.Status switch
                {
                    AudioPlaybackConnectionOpenResultStatus.Success =>
                        "O celular encerrou a conexão durante a abertura. Tente novamente.",
                    AudioPlaybackConnectionOpenResultStatus.RequestTimedOut =>
                        "O celular demorou a responder. Mantenha-o próximo e tente novamente.",
                    AudioPlaybackConnectionOpenResultStatus.DeniedBySystem =>
                        "O Windows recusou a conexão. Confira o Bluetooth e o emparelhamento.",
                    _ => "Não foi possível abrir a conexão de áudio."
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
            detail = devices.ContainsKey(device.Id)
                ? "A tentativa de conexão foi cancelada."
                : "O celular ficou indisponível durante a conexão.";
        }
        catch (Exception error)
        {
            failure = error;
            detail = "Falha na conexão. Confira o Bluetooth e o emparelhamento.";
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
            if (status == ConnectionStatus.Error)
            {
                RecordDiagnostic(stage, detail, failure, resultStatus);
            }
            SetStatus(status, WithErrorCode(detail, failure));
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
                    const string message = "A conexão foi encerrada. Confira o Bluetooth nos dois aparelhos.";
                    RecordDiagnostic("Acompanhar conexão de áudio", message);
                    SetStatus(ConnectionStatus.Disconnected, message);
                    UpdateControls();
                }
            }
            catch (Exception error)
            {
                ReleaseConnection();
                RecordDiagnostic("Consultar conexão de áudio", "Não foi possível consultar a conexão.", error);
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
        StopWatchingAudioOutput();
        StopDeviceWatcher();
        windowLifetime.Cancel();
        ReleaseConnection();
        windowLifetime.Dispose();
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedName.Text = (DeviceList.SelectedItem as DeviceInformation)?.Name ?? "Nenhum celular selecionado";
        if (!updatingDeviceList && connection is null && connectionAttempt is null)
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
            RecordDiagnostic("Abrir configurações de Bluetooth", "O Windows não abriu as configurações.", error);
            MessageBox.Show(this, WithErrorCode("Abra Configurações > Bluetooth no Windows.", error),
                "Configurações de Bluetooth", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RecordDiagnostic(string stage, string detail, Exception? error = null, string? result = null)
    {
        var lines = new List<string> { $"Etapa: {stage}", $"Detalhe: {detail}" };
        if (result is not null)
        {
            lines.Add($"Resultado do Windows: {result}");
        }
        if (error is not null)
        {
            lines.Add($"Código: 0x{error.HResult:X8}");
        }
        lastDiagnostic = string.Join(Environment.NewLine, lines);
        DiagnosticFeedback.Text = "";
    }

    private void CopyDiagnosticClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildDiagnosticReport());
            DiagnosticFeedback.Text = "Diagnóstico copiado.";
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            DiagnosticFeedback.Text = "Não foi possível copiar. Tente novamente.";
        }
    }

    private string BuildDiagnosticReport() => string.Join(Environment.NewLine,
        $"Toca no PC {typeof(MainWindow).Assembly.GetName().Version}",
        $"Windows {Environment.OSVersion.Version}",
        $".NET {Environment.Version}",
        $"Estado atual: {StatusText.Text}",
        "Última ocorrência:",
        lastDiagnostic ?? "Nenhuma ocorrência registrada.");

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

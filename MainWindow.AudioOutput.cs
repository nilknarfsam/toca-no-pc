using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace PcAudio;

public partial class MainWindow
{
    private bool watchingAudioOutput;
    private int audioOutputQuery;

    private void StartWatchingAudioOutput()
    {
        if (!watchingAudioOutput)
        {
            try
            {
                MediaDevice.DefaultAudioRenderDeviceChanged += DefaultAudioOutputChanged;
                watchingAudioOutput = true;
            }
            catch (Exception error)
            {
                Trace.TraceWarning("Falha ao acompanhar a saída padrão do Windows ({0:X8}).", error.HResult);
            }
        }

        _ = UpdateDefaultAudioOutputAsync();
    }

    private void StopWatchingAudioOutput()
    {
        if (watchingAudioOutput)
        {
            try
            {
                MediaDevice.DefaultAudioRenderDeviceChanged -= DefaultAudioOutputChanged;
            }
            catch (Exception error)
            {
                Trace.TraceWarning("Falha ao parar o acompanhamento da saída padrão ({0:X8}).", error.HResult);
            }
            watchingAudioOutput = false;
        }
    }

    private void DefaultAudioOutputChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        if (args.Role == AudioDeviceRole.Default)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (!closed)
                {
                    _ = UpdateDefaultAudioOutputAsync();
                }
            });
        }
    }

    private async Task UpdateDefaultAudioOutputAsync()
    {
        var query = ++audioOutputQuery;
        string label;
        try
        {
            var id = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            var output = string.IsNullOrEmpty(id)
                ? null
                : await DeviceInformation.CreateFromIdAsync(id).AsTask(windowLifetime.Token);
            label = output is null || string.IsNullOrWhiteSpace(output.Name)
                ? "Nenhuma saída padrão identificada. Confira a saída em Configurações de Som."
                : $"Saída padrão do Windows: {output.Name}";
        }
        catch (Exception error)
        {
            if (closed || query != audioOutputQuery)
            {
                return;
            }

            Trace.TraceInformation("Não foi possível consultar a saída padrão ({0:X8}).", error.HResult);
            label = "Confira a saída atual em Configurações de Som do Windows.";
        }

        if (!closed && query == audioOutputQuery)
        {
            DefaultAudioOutputText.Text = label;
        }
    }
}

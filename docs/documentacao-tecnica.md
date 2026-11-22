# Documentação técnica

## Arquitetura

O Toca no PC é um aplicativo WPF em C# com alvo `net9.0-windows10.0.19041.0`. Ele usa a projeção WinRT incluída no SDK para acessar `Windows.Media.Audio.AudioPlaybackConnection`. O executável desktop é carregado sem MSIX e funciona com a conta padrão do usuário. O Windows oferece a API a partir da build 19041. [Integração WinRT no desktop](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps) · [Referência da API](https://learn.microsoft.com/en-us/uwp/api/windows.media.audio.audioplaybackconnection?view=winrt-26100)

**Fluxo de áudio** · Celular (A2DP Source) → Windows (A2DP Sink) → saída de reprodução do Windows → Audacity ou REAPER (WASAPI loopback).

**Fluxo de controle** · A janela consulta `AudioPlaybackConnection.GetDeviceSelector()` e passa o seletor a `DeviceInformation.FindAllAsync()`. Ao conectar, o app chama `TryCreateFromId`, registra `StateChanged` e aguarda `StartAsync` e `OpenAsync`. O estado **Conectado** exige um resultado de sucesso e uma conexão aberta. O app mantém uma conexão por vez. [Fluxo recomendado pela Microsoft](https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/enable-remote-audio-playback)

| Estado | Condição |
| --- | --- |
| Desconectado | Nenhuma sessão aberta, cancelamento concluído ou sessão encerrada. |
| Conectando | Preparação, abertura ou cancelamento em andamento. |
| Conectado | O Windows concluiu a abertura da conexão A2DP. |
| Erro | Bluetooth indisponível, falha de enumeração ou falha de abertura, entre outras condições descritas na interface. |

Os estados visuais são atualizados na thread da janela. Ao encerrar a sessão, o app remove o manipulador de evento e libera a conexão. Fechar a janela também solicita o cancelamento das operações pendentes. A verificação do rádio consulta o estado do adaptador e o emparelhamento é feito nas configurações do Windows.

## Arquivos principais

| Arquivo | Responsabilidade |
| --- | --- |
| [PcAudio.csproj](../PcAudio.csproj) | Alvo Windows, configuração WPF e metadados do executável. |
| [App.xaml](../App.xaml) | Inicialização do aplicativo. |
| [MainWindow.xaml](../MainWindow.xaml) | Interface, ações de conexão e ajuda rápida. |
| [MainWindow.xaml.cs](../MainWindow.xaml.cs) | Enumeração, ciclo de vida da conexão, estados e tratamento de erros. |
| [Assets/TocaNoPc.ico](../Assets/TocaNoPc.ico) | Ícone da janela e do executável. |

## Limites de integração

- O Windows decodifica e reproduz o áudio recebido. A API controla a sessão e não expõe ao aplicativo amostras, entrada de microfone ou escolha de saída por conexão.
- O loopback grava o áudio misturado na saída escolhida. Outros aplicativos que usam a mesma saída podem entrar na gravação. [WASAPI loopback](https://learn.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording)
- O A2DP pode introduzir atraso variável e compressão com perdas. Exportar em WAV preserva o sinal capturado pelo gravador. O fluxo não foi validado para monitoração de apresentações ao vivo. [Bluetooth Classic Audio](https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/bluetooth-classic-audio)

## Validação

O ambiente de desenvolvimento usado para o teste foi Windows 11 25H2, SDK do Windows 10.0.26100.0 e SDK do .NET 9.0.317. No teste com hardware, o usuário confirmou conexão, reprodução e gravação de um trecho. O gravador usado nesse teste não foi informado. Na configuração do Audacity 4, foi possível trocar a entrada de microfone pelo loopback dos alto-falantes.

Para repetir a validação:

1. Compile em Release, abra o executável e confirme que a janela carrega e atualiza a lista.
2. Selecione o celular, conecte e confirme que o áudio reproduzido nele toca no PC.
3. Grave um trecho em cada gravador pelo [guia de uso](guia-de-uso.md). Pause o celular e confira se o trecho gravado tem o áudio esperado.
4. Desconecte no app, reconecte e desligue o Bluetooth no celular durante a reprodução. Confira se a sessão encerra e a janela permite conectar novamente.
5. Restabeleça a sessão e feche a janela enquanto o celular toca. Confirme que o áudio deixa de tocar no PC e que uma nova instância pode conectar.

Se o NuGet não puder usar o cache padrão, defina o cache dentro da pasta do projeto apenas para a sessão atual do PowerShell:

```powershell
$env:NUGET_PACKAGES = Join-Path $PWD 'obj\packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $PWD 'obj\nuget-http'
dotnet build -c Release
```

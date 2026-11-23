# Documentação técnica

## Arquitetura

O Toca no PC é um aplicativo WPF em C# com alvo `net10.0-windows10.0.19041.0`. Ele usa a projeção WinRT incluída no SDK para acessar `Windows.Media.Audio.AudioPlaybackConnection`. O executável desktop é carregado sem MSIX e funciona com a conta padrão do usuário. O Windows oferece a API a partir da build 19041. [Integração WinRT no desktop](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps) · [Referência da API](https://learn.microsoft.com/en-us/uwp/api/windows.media.audio.audioplaybackconnection?view=winrt-26100)

**Fluxo de áudio** · Celular (A2DP Source) → Windows (A2DP Sink) → saída de reprodução do Windows → Audacity ou REAPER (WASAPI loopback).

**Fluxo de controle** · A janela inicia um `DeviceWatcher` com o seletor de `AudioPlaybackConnection.GetDeviceSelector()` para receber a lista inicial e acompanhar inclusões, alterações e remoções. O botão **Atualizar** inicia uma nova busca. Ao conectar, o app chama `TryCreateFromId`, registra `StateChanged` e aguarda `StartAsync` e `OpenAsync`. O estado **Conectado** exige um resultado de sucesso e uma conexão aberta. O app mantém uma conexão por vez. [Fluxo recomendado pela Microsoft](https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/enable-remote-audio-playback)

| Estado | Condição |
| --- | --- |
| Desconectado | Nenhuma sessão aberta, cancelamento concluído ou sessão encerrada. |
| Conectando | Preparação, abertura ou cancelamento em andamento. |
| Conectado | O Windows concluiu a abertura da conexão A2DP. |
| Erro | Bluetooth indisponível, falha de enumeração ou falha de abertura, entre outras condições descritas na interface. |

Os estados visuais e a lista são atualizados na thread da janela. A seleção é preservada enquanto o dispositivo aparece na lista. Se o Windows remover o celular selecionado durante uma tentativa ou sessão ativa, o app cancela a tentativa ou encerra a sessão. Fechar a janela para a busca, solicita o cancelamento das operações pendentes e libera a conexão. O emparelhamento é feito nas configurações do Windows.

Se o observador falhar durante a enumeração, a janela descarta os resultados parciais e limpa a lista antes de liberar **Atualizar**. Se ele parar após concluir a enumeração, a janela conserva a última lista válida e exibe um aviso. Cada nova busca ignora os eventos pendentes da anterior.

A saída exibida vem de `MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default)`. O app consulta o nome do dispositivo e acompanha mudanças da saída padrão. O diagnóstico copiável guarda em memória a última ocorrência da sessão e inclui versões, estado e, quando disponível, resultado WinRT e HRESULT. A saída e os identificadores de dispositivos ficam fora do relatório. [API MediaDevice](https://learn.microsoft.com/en-us/uwp/api/windows.media.devices.mediadevice)

## Arquivos principais

| Arquivo | Responsabilidade |
| --- | --- |
| [PcAudio.csproj](../PcAudio.csproj) | Alvo Windows, configuração WPF e metadados do executável. |
| [App.xaml](../App.xaml) | Inicialização do aplicativo. |
| [MainWindow.xaml](../MainWindow.xaml) | Interface, ações de conexão e ajuda rápida. |
| [MainWindow.xaml.cs](../MainWindow.xaml.cs) | Ciclo de vida da conexão, estados e tratamento de erros. |
| [MainWindow.Devices.cs](../MainWindow.Devices.cs) | Busca inicial, atualização automática da lista e parada do observador de dispositivos. |
| [MainWindow.AudioOutput.cs](../MainWindow.AudioOutput.cs) | Consulta e acompanhamento da saída padrão de mídia. |
| [windows.yml](../.github/workflows/windows.yml) | Compilação e pacote autossuficiente Windows x64 no GitHub Actions. |
| [Assets/TocaNoPc.ico](../Assets/TocaNoPc.ico) | Ícone da janela e do executável. |

## Limites de integração

- O Windows decodifica e reproduz o áudio recebido. A API controla a sessão e não expõe ao aplicativo amostras, entrada de microfone ou escolha de saída por conexão.
- O loopback grava o áudio misturado na saída escolhida. Outros aplicativos que usam a mesma saída podem entrar na gravação. [WASAPI loopback](https://learn.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording)
- O A2DP pode introduzir atraso variável e compressão com perdas. Exportar em WAV preserva o sinal capturado pelo gravador. O fluxo não foi validado para monitoração de apresentações ao vivo. [Bluetooth Classic Audio](https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/bluetooth-classic-audio)

## Validação

Antes da migração, o usuário confirmou conexão, reprodução e gravação de um trecho em Windows 11 25H2 com .NET 9. O gravador usado nesse teste ainda está por identificar. Na configuração do Audacity 4, foi possível trocar a entrada de microfone pelo loopback dos alto-falantes.

Na validação local da versão 1.1.0, o SDK .NET 10.0.404 compilou a versão Release sem avisos nem erros e o pacote autossuficiente x64 abriu a janela principal. Um teste WPF com falha de busca simulada confirmou a remoção do item antigo, a liberação de **Atualizar**, a etapa e o HRESULT no diagnóstico e a exclusão de nomes, IDs e mensagens brutas. O mesmo teste confirmou a consulta do nome da saída padrão. A rodada Bluetooth com celular, a troca de saída e o teste em Windows limpo permanecem pendentes.

Para repetir a validação:

1. Compile em Release e abra o executável. Confira se a lista carrega. Com o app aberto, emparelhe uma fonte A2DP ou remova um emparelhamento de teste nas configurações do Windows e confira a mudança na lista sem clicar em **Atualizar**.
2. Selecione o celular, conecte e confirme que o áudio reproduzido nele toca no PC.
3. Grave um trecho em cada gravador pelo [guia de uso](guia-de-uso.md). Pause o celular e confira se o trecho gravado tem o áudio esperado.
4. Desconecte no app, reconecte e desligue o Bluetooth no celular durante a reprodução. Confira se a sessão encerra e a janela permite conectar novamente.
5. Cancele uma tentativa com **Desconectar**. Teste também a perda de alcance durante a reprodução. Restabeleça a sessão e feche a janela enquanto o celular toca. Confirme que o áudio para e que uma nova instância pode conectar.
6. Troque a saída padrão em **Configurações → Sistema → Som** e confira se a indicação acompanha a mudança. Em caso de falha, use **Copiar diagnóstico** e confira a etapa, as versões e a ausência de nomes e IDs de dispositivos.
7. Em um Windows de teste sem .NET Desktop Runtime 10, extraia o pacote x64 e repita o fluxo de conexão e reprodução como usuário padrão.

Registre a versão do app, a edição e build do Windows, o adaptador Bluetooth, o sistema do celular, a saída usada e a versão de cada gravador. Para cada cenário, registre o resultado, a mensagem exibida e se foi possível recuperar a conexão.

Se o NuGet não puder usar o cache padrão, defina o cache dentro da pasta do projeto apenas para a sessão atual do PowerShell:

```powershell
$env:NUGET_PACKAGES = Join-Path $PWD 'obj\packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $PWD 'obj\nuget-http'
dotnet build -c Release
```

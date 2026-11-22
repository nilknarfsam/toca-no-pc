# Guia de uso do Toca no PC

## Emparelhar e enviar áudio ao PC

1. Ligue o Bluetooth nos dois aparelhos e mantenha aberta a tela de Bluetooth no celular.
2. No app, clique em **Configurações de Bluetooth**. No Windows 11, use **Bluetooth e dispositivos → Adicionar dispositivo → Bluetooth**. No Windows 10, use **Dispositivos → Bluetooth e outros dispositivos → Adicionar Bluetooth ou outro dispositivo → Bluetooth**.
3. Escolha o celular e confirme o código correspondente nos dois aparelhos, se solicitado.
4. Volte ao app e aguarde o Windows detectar o celular na lista. Selecione-o, clique em **Conectar** e aguarde **Conectado**. Se necessário, clique em **Atualizar** para iniciar outra busca.
5. Reproduza um áudio conhecido no celular. Se o som continuar no aparelho, abra o seletor de saída de mídia da reprodução e selecione o PC, se ele aparecer. No Android, confira se **Áudio de mídia** está ativado para o PC quando essa opção existir. No iPhone, procure o PC na lista de saídas do controle de reprodução da Central de Controle. Ouça o som no PC antes de abrir o gravador.
6. Use **Desconectar** para encerrar a recepção ou cancelar uma tentativa em andamento. Também é possível fechar a janela.

Se a lista estiver vazia, confirme o emparelhamento, mantenha o celular próximo e atualize a lista. A lista mostra as fontes A2DP que o seletor nativo do Windows expõe. [Guia de emparelhamento da Microsoft](https://support.microsoft.com/en-gb/windows/hardware/bluetooth/pair-a-bluetooth-device-in-windows)

## Gravação por WASAPI loopback

Escolha no gravador a saída pela qual você ouve o celular no PC. O loopback pode gravar notificações, navegador e outros sons reproduzidos pela mesma saída. Pause outras fontes e desative o monitoramento da entrada no gravador para evitar realimentação e eco. [Como funciona o loopback WASAPI](https://learn.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording)

### Audacity 4

1. Abra **Config. do áudio → Configurações do áudio**. Em **Host**, escolha **Windows WASAPI**.
2. Em **Dispositivo de gravação**, escolha a entrada **(loopback)** com o nome da saída em que ouve o celular. Por exemplo, **Alto-falantes (nome do dispositivo) (loopback)**. Use **2 canais de gravação (estéreo)**.
3. Nas preferências do Audacity 4, abra **Reprodução/Gravação** e deixe **Ative o monitoramento de entrada** desmarcado. No Audacity 3, procure o monitoramento audível em **Transporte → Opções de transporte**.
4. Confirme com **OK**, dê play no celular e clique em **Gravar** no Audacity. Grave de 10 a 15 segundos. Pause o celular antes de ouvir o trecho.

O seletor de entrada do Audacity mostra as saídas com o sufixo **(loopback)**. Procure pelo nome dos alto-falantes ou fones do PC. [Guia do Audacity 4](https://support.audacityteam.org/au4/basics/recording-desktop-audio) · [Manual do Audacity 3](https://manual.audacityteam.org/man/tutorial_recording_computer_playback_on_windows.html)

### REAPER

1. Abra um projeto novo. Em **Project Settings** (`Alt+Enter`) → **Advanced**, o guia oficial orienta habilitar **Allow feedback routing** para esse fluxo. Faça isso no projeto de teste e mantenha a saída da faixa silenciada durante a captura.
2. Em **Options → Preferences → Audio → Device**, selecione **WASAPI** em **Audio system** e **Shared loopback (CAUTION)** em **Mode**.
3. Em **Output device**, selecione a saída em que você ouve o celular. Selecione **2** em **Input channels** para captura estéreo. Se o dispositivo falhar ao abrir, confira a taxa de amostragem e o tamanho do bloco nas configurações de áudio.
4. Crie uma faixa e arme-a para gravação. Escolha o par em **Input: Stereo**. Desligue **Record monitoring** no botão de alto-falante da faixa e silencie a saída dessa faixa durante a gravação.
5. Reproduza o áudio no celular, pressione `Ctrl+R` no REAPER e grave alguns segundos. Pare a gravação, desarme a faixa, pause o celular e retire o mudo da faixa para conferir o trecho.

Passos conferidos na seção 3.44 do [guia do REAPER 7.86](https://dlx.reaper.fm/userguide/ReaperUserGuide786c.pdf).

## Solução de problemas

| Sintoma | O que verificar |
| --- | --- |
| O celular ficou fora da lista | Ligue o Bluetooth nos dois aparelhos, confirme o emparelhamento nas configurações do Windows e clique em **Atualizar**. O celular precisa oferecer áudio de mídia por A2DP. |
| O app mostra **Conectado**, mas o celular toca no próprio alto-falante | Abra o seletor de saída de mídia do celular e escolha o PC. Confira volume e saída em **Configurações → Sistema → Som** no Windows. |
| O Audacity lista o microfone, mas não mostra o loopback esperado | Confira **Windows WASAPI** em **Host**. No Audacity 4, use **Config. do áudio → Verificar novos disp. de áudio** e procure de novo pela saída. |
| O Audacity grava silêncio ou sons do ambiente | Selecione o **(loopback)** da mesma saída que está reproduzindo a música. Dê play primeiro no celular e comece o teste em uma faixa estéreo. |
| A gravação tem eco | Desative o monitoramento da entrada. No REAPER, silencie a saída da faixa enquanto grava em modo loopback. |


# Toca no PC

Ouça no Windows o áudio de mídia do seu celular por Bluetooth. Para gravar o que está tocando, use Audacity ou REAPER com WASAPI loopback.

O aplicativo usa o receptor A2DP nativo do Windows. Ele acompanha as fontes de áudio emparelhadas que o Windows disponibiliza, abre e encerra uma conexão por vez e mostra os estados **Desconectado**, **Conectando**, **Conectado** e **Erro**.

## Requisitos

- Windows 11 ou Windows 10 de edição compatível, com Bluetooth clássico, uma saída de som e build 19041 ou posterior para a API Bluetooth. Confira as [versões do Windows com suporte ao .NET 10](https://learn.microsoft.com/dotnet/core/install/windows).
- Celular com suporte ao perfil de fonte A2DP.
- SDK do .NET 10 LTS para compilar ou .NET Desktop Runtime 10 para executar a compilação comum.
- Audacity ou REAPER, se quiser gravar.

## Compilar e executar

Clone o repositório e execute estes comandos no PowerShell:

```powershell
git clone https://github.com/nilknarfsam/toca-no-pc.git
cd toca-no-pc
dotnet build -c Release
.\bin\Release\net10.0-windows10.0.19041.0\TocaNoPc.exe
```

Também é possível iniciar pela raiz com `dotnet run`. A primeira compilação pode baixar a referência WinRT do NuGet. O executável gerado depende do .NET Desktop Runtime 10.

Para montar um pacote Windows x64 com o runtime incluído, execute:

```powershell
dotnet publish PcAudio.csproj -c Release -r win-x64 --self-contained true -o bin/publicacao/win-x64
```

Abra `TocaNoPc.exe` na pasta publicada e mantenha os arquivos juntos. O [GitHub Actions](https://github.com/nilknarfsam/toca-no-pc/actions) também compila esse pacote a cada envio ao branch principal e a cada pull request para ele.

## Ouvir e gravar

1. Abra **Configurações de Bluetooth** no Toca no PC e emparelhe o celular no Windows.
2. Volte ao app, selecione o celular na lista e clique em **Conectar**. O app atualiza a lista automaticamente. Use **Atualizar** se precisar reiniciar a busca.
3. Dê play no celular e verifique se o som sai pelo PC. Use o seletor de saída de mídia do celular se precisar direcionar o áudio para o computador.
4. Confira o nome em **Saída padrão do Windows** no app. No **Audacity**, selecione **Windows WASAPI** e escolha a entrada **(loopback)** da saída em que escuta o celular.
5. No **REAPER**, selecione **WASAPI → Shared loopback** e use a mesma saída em **Output device**. Arme uma faixa com entrada estéreo.
6. Desative o monitoramento de entrada no gravador para evitar realimentação. O loopback pode incluir notificações e outros sons da mesma saída.

Se houver falha, use **Copiar diagnóstico** para obter a etapa, o código quando disponível e as versões do app e do sistema. O relatório omite nomes e IDs de dispositivos.

## O que foi verificado

Em teste com um celular real, o usuário confirmou que conectou o aparelho, ouviu o áudio no PC e gravou um trecho com sucesso. O Audacity 4 exibiu e aceitou a entrada de loopback dos alto-falantes durante a configuração. O gravador usado no teste bem-sucedido ainda precisa ser identificado para registrar o resultado por programa.

## Documentação

- [Guia de uso e solução de problemas](docs/guia-de-uso.md)
- [Documentação técnica e roteiro de validação](docs/documentacao-tecnica.md)
- [Plano de melhorias](docs/plano-de-melhorias.md)

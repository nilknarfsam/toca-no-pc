# Toca no PC

Ouça no Windows o áudio de mídia do seu celular por Bluetooth. Para gravar o que está tocando, use Audacity ou REAPER com WASAPI loopback.

O aplicativo usa o receptor A2DP nativo do Windows. Ele acompanha as fontes de áudio emparelhadas que o Windows disponibiliza, abre e encerra uma conexão por vez e mostra os estados **Desconectado**, **Conectando**, **Conectado** e **Erro**.

## Requisitos

- Windows 11 ou Windows 10 a partir da versão 2004, com Bluetooth clássico e uma saída de som.
- Celular com suporte ao perfil de fonte A2DP.
- SDK do .NET 9 para compilar ou .NET Desktop Runtime 9 para executar o resultado da compilação.
- Audacity ou REAPER, se quiser gravar.

O .NET 9 saiu de suporte. A migração para o .NET 10 LTS está prevista antes da distribuição do executável. [Ciclo de suporte do .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

## Compilar e executar

Clone o repositório e execute estes comandos no PowerShell:

```powershell
git clone https://github.com/nilknarfsam/toca-no-pc.git
cd toca-no-pc
dotnet build -c Release
.\bin\Release\net9.0-windows10.0.19041.0\TocaNoPc.exe
```

Também é possível iniciar pela raiz com `dotnet run`. A primeira compilação pode baixar a referência WinRT do NuGet. O executável gerado depende do .NET Desktop Runtime 9.

## Ouvir e gravar

1. Abra **Configurações de Bluetooth** no Toca no PC e emparelhe o celular no Windows.
2. Volte ao app, selecione o celular na lista e clique em **Conectar**. O app atualiza a lista automaticamente. Use **Atualizar** se precisar reiniciar a busca.
3. Dê play no celular e verifique se o som sai pelo PC. Use o seletor de saída de mídia do celular se precisar direcionar o áudio para o computador.
4. No **Audacity**, selecione **Windows WASAPI** e, em **Dispositivo de gravação**, escolha a entrada **(loopback)** com o mesmo nome da saída em que escuta o celular.
5. No **REAPER**, selecione **WASAPI → Shared loopback** e use a mesma saída em **Output device**. Arme uma faixa com entrada estéreo.
6. Desative o monitoramento de entrada no gravador para evitar realimentação. O loopback pode incluir notificações e outros sons da mesma saída.

## O que foi verificado

Em teste com um celular real, o usuário confirmou que conectou o aparelho, ouviu o áudio no PC e gravou um trecho com sucesso. O Audacity 4 exibiu e aceitou a entrada de loopback dos alto-falantes durante a configuração. O gravador usado no teste bem-sucedido ainda precisa ser identificado para registrar o resultado por programa.

## Documentação

- [Guia de uso e solução de problemas](docs/guia-de-uso.md)
- [Documentação técnica e roteiro de validação](docs/documentacao-tecnica.md)
- [Plano de melhorias](docs/plano-de-melhorias.md)

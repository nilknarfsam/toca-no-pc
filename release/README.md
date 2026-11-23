# Pacotes Windows

Esta pasta guarda os ZIPs por versão, no formato `TocaNoPc-<versão>-win-x64.zip`. O número vem da propriedade `Version` de [PcAudio.csproj](../PcAudio.csproj).

Com o SDK do .NET 10 e PowerShell 7, execute `./release/New-Release.ps1` a partir da raiz do projeto. Ao gerar outra vez o pacote da mesma versão, o script substitui o ZIP depois que a compilação e a compactação terminam. Os ZIPs das outras versões permanecem na pasta.

Os ZIPs locais ficam fora do controle de versão. No GitHub, baixe o artefato do [build](https://github.com/nilknarfsam/toca-no-pc/actions), extraia o ZIP versionado com todos os arquivos e execute `TocaNoPc.exe`. O pacote inclui o runtime.

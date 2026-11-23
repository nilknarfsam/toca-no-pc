# Plano de melhorias

## Entregue na versão 1.1.0

- A lista acompanha os eventos de dispositivos enviados pelo Windows. Se a enumeração falhar durante uma atualização, os resultados são descartados e **Atualizar** permite tentar outra vez.
- **Copiar diagnóstico** inclui etapa, versões e código quando disponível. Nomes e identificadores de dispositivos ficam fora do relatório.
- A ajuda de gravação mostra a saída padrão do Windows para facilitar a escolha da entrada de loopback.
- O app usa .NET 10 LTS. O fluxo do GitHub Actions compila um artefato Windows x64 com o runtime incluído.

## Próxima rodada · Validar em hardware

- Registrar conexão, reprodução e gravação em cada gravador, com a saída usada.
- Registrar cancelamento, desconexão, perda de alcance, fechamento durante a reprodução e recuperação da conexão.
- Confirmar a atualização do nome da saída ao trocar o dispositivo padrão do Windows.
- Repetir em um Windows limpo com o pacote x64 e registrar a combinação de Windows, adaptador e celular que passou.

Use o [roteiro da documentação técnica](documentacao-tecnica.md#validação) e o [guia de uso](guia-de-uso.md).

## Depois da validação · Distribuir

- Preparar notas de versão, instruções de instalação e o download no GitHub Releases.
- Avaliar a assinatura do executável e outras arquiteturas conforme houver hardware para validar.

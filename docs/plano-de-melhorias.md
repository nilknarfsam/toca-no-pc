# Plano de melhorias

## Prioridade 1 · Validar a gravação de ponta a ponta

- Gravar o mesmo trecho via Bluetooth no Audacity e no REAPER e confirmar o conteúdo de cada gravação.
- Repetir os testes de cancelar, desconectar, perder o alcance e fechar o app durante a reprodução.
- Registrar versão do Windows, adaptador, sistema do celular e saída do PC usada no teste.

**Conclusão** · Recepção, gravação e reconexão verificadas em hardware, com os passos reproduzíveis no [guia de uso](guia-de-uso.md).

## Prioridade 2 · Facilitar a conexão e o diagnóstico

- Atualizar a lista por eventos de adição e remoção de dispositivos.
- Indicar quando é preciso ligar o Bluetooth, atualizar a lista ou selecionar a entrada **(loopback)** no gravador.
- Oferecer diagnóstico copiável com etapa da falha, código do erro e versões relevantes, sem incluir nomes ou identificadores dos dispositivos por padrão.

**Conclusão** · A janela reage ao pareamento e à remoção de uma fonte, e as falhas mais comuns orientam o usuário sobre a próxima ação.

## Prioridade 3 · Preparar uma distribuição

- Migrar para o .NET 10 LTS e repetir os testes com hardware.
- Montar um fluxo de compilação em Windows para as arquiteturas que forem efetivamente testadas.
- Experimentar um pacote autossuficiente com o runtime incluído, verificar numa instalação limpa do Windows e decidir o formato de distribuição.
- Preparar notas de versão, instruções de instalação e download para o GitHub Releases. Avaliar assinatura do executável antes da divulgação no site.

**Conclusão** · Uma pessoa consegue baixar, executar e conectar o celular em um Windows de teste, seguindo as instruções públicas.

# Perfis de Aeronaves Personalizadas (HubHop / L-Vars)

Este diretório contém perfis adicionais de aeronaves para o **Flight Recorder**.

## Como funciona?
O Flight Recorder utiliza o suporte **100% nativo a L-Vars do SimConnect**, sem necessidade de instalar nada na pasta `Community` do MSFS.

Ao carregar ou gravar uma aeronave:
1. O Flight Recorder lê o título da aeronave (`TITLE` no SimConnect).
2. Se o título corresponder a um dos perfis embutidos (**FlyByWire A32NX**, **PMDG 737**, **Fenix A320**, **iniBuilds**) ou a qualquer arquivo `.json` nesta pasta, as variáveis personalizadas (L-Vars) são gravadas e reproduzidas automaticamente.
3. As superfícies complexas (flaps, slats, speedbrakes/spoilers, reversores de empuxo, trem de pouso e luzes) responderão perfeitamente durante o replay.

## Criando seu próprio perfil com o HubHop:
Basta criar um arquivo `.json` nesta pasta seguindo o modelo abaixo:

```json
{
  "id": "CUSTOM_PLANE",
  "name": "Meu Aviao Customizado",
  "vendor": "Desenvolvedor",
  "matchTitles": ["NomeParcialNoSimulador"],
  "variables": [
    {
      "name": "L:CUSTOM_FLAPS_INDEX",
      "unit": "number",
      "description": "Posicao dos Flaps",
      "interpolate": false
    },
    {
      "name": "L:CUSTOM_SPOILER_POS",
      "unit": "number",
      "description": "Posicao dos Spoilers",
      "interpolate": true
    }
  ]
}
```
*(Máximo de 32 variáveis por perfil)*

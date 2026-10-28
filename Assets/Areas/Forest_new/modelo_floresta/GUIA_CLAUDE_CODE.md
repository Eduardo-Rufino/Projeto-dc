# Modelo base — Mapa da Floresta (para o Claude Code montar no Godot)

Este pacote é um **exemplo completo** de mapa de exploração: o layout inteiro em JSON, os sprites (kit),
prévias de como deve ficar e um script de carga de referência em C#. A ideia é usar este mapa como
padrão para os próximos (caverna, deserto, pântano).

## Estrutura

```
modelo_floresta/
  GUIA_CLAUDE_CODE.md         <- este arquivo
  kit/                        <- sprites + manifest.json (nomes, frames, âncoras)
    manifest.json, LEIA-ME.txt
    tile_sheets/  terrain/  overlays/  prop_sheets/  props/  tiles/  godot/tree_fade.gd  opcional/
  mapa/
    mapa_floresta.json        <- o mapa inteiro (terreno, altura, andar, objetos, bloqueios, quests, spots)
    previas/                  <- como o mapa DEVE ficar (bloqueado / liberado, planta vista de cima, detalhes)
    godot/MapLoader.cs        <- REFERÊNCIA (não compilada/testada) de como carregar o JSON
  ferramentas/montar_previa.py  <- remonta a prévia só a partir do JSON + kit (Python + Pillow), para conferir
```

## Regras visuais (não mudar)

- Losango do bloco: 64×32 px nativos. **Mundo em escala 2**, digimons em escala 3. Filtro **Nearest** em tudo.
- Sem efeitos de iluminação (decisão do projeto). A pasta `kit/opcional/` não é usada.
- Projeção (pixels nativos, origem = centro do bloco (0,0) no nível da grama):
  - `X = (gx - gy) * 32` , `Y = (gx + gy) * 16`
  - superfícies ficam mais baixas: areia +4, terra +6, água +8 px; cada nível de elevação sobe 18 px.
  - inversa (use a posição **no chão**): `gx = round((X/32 + Y/16)/2)`, `gy = round((Y/16 - X/32)/2)`.
- Todo `pos` no JSON já é o ponto de âncora final (com offset de superfície e elevação).
  `sort_y` é o Y **do chão**, usado para ordenar profundidade.

## Como montar a cena (recomendado)

Gerar a cena a partir do JSON (como o `MapLoader.cs` faz), em vez de posicionar 9 mil sprites à mão:

1. Nó raiz `Node2D` com `Scale = (2,2)` e `TextureFilter = Nearest`.
2. Camada **Ground** (sem Y-sort): os `tiles` com `layer == "ground"`, **na ordem em que aparecem no JSON**
   (já vem ordenado de trás para frente). Cada bloco: sprite com `offset = -tile_center` na posição `pos`.
   Os `overlays` do bloco vão por cima, no mesmo lugar (o `fringe_*` tem `offset_y` de -6).
3. Camada **Shadows**: sombras dos objetos (`shadow` = nome do sprite em `kit/overlays`), centradas em
   `(pos.x + shadow_offset.x, pos.y - 2)`.
4. Camada **Sorted** com `YSortEnabled = true`: blocos altos e rampas (`layer == "sorted"`), todos os
   `objects`, os objetos dos bloqueios/mina, NPCs, inimigos e o player.
   - Cada item é um `Node2D` em `(pos.x, sort_y)` e o sprite filho tem
     `offset = -anchor + (0, pos.y - sort_y)` → desenha na altura certa, mas ordena pelo chão.
   - Empates: adicionar na ordem do JSON.
5. Animações: blocos de grama/água 4 quadros a 5 fps começando em `(gx+gy) % 4`; árvores e props com
   `frame_mode = "wind"` a ~3,5 fps começando em `(gx+gy)//4 % 4` (grupos balançam juntos); o resto 4 fps.

A prova de que a regra está certa: `ferramentas/montar_previa.py` monta a imagem só com estas regras e
bate com `mapa/previas/mapa_*_1x.png` (diferença de 0,1%, que são só os ícones dos spots).

## Andar / colisão

- `walk` (uma string por linha, `walk[gy][gx]`):
  `.` anda · `#` não anda (floresta, água) · `R` rampa · `Q` bloqueio de quest · `O` objeto sólido.
- `elevation` (0 ou 1). Só se passa entre blocos de **mesmo nível**, exceto pela rampa:
  a rampa liga **só** o vizinho baixo (lado da frente) ao vizinho alto (`rises_toward` diz para onde sobe:
  `-gx` = sobe para cima-esquerda da tela, `-gy` = sobe para cima-direita). As laterais da rampa são paredes.
- `Q`: bloqueado até a quest do bloqueio (`blockers[].quest`) ser concluída. No rio, depois da jangada,
  as casas `Q` viram passagem (a balsa).
- O `MapLoader.cs` tem `CanStep(a, b)` com essas regras e `VisualLift()` para subir o desenho do digimon
  em planaltos (18 px) e rampas (gradual). O player deve ficar dentro de **Sorted** com `Position` = posição no chão.
- Para NPCs/inimigos que andam sozinhos, dá para montar um `AStarGrid2D` com o mesmo `CanStep`.

## Quests, bloqueios e transições

| Quest | NPC (spot) | Precisa | Libera | Visual |
|---|---|---|---|---|
| `q_lenhador` | `npc_lenhador` | — | `bloqueio_arvores` (sul da praça) | pilha de toras → tocos; dá `tabuas` |
| `q_jangada` | `npc_construtor` | `tabuas` | `bloqueio_rio` | aparece a balsa; leva à margem do pântano |
| `q_escavador` | `npc_escavador` | `chip_escavacao` (spot `item_chip`, beco ao sul) | `bloqueio_deslizamento` | some o deslizamento, aparece a **mina** ao lado do caminho |

- Cada bloqueio tem `objects_while_blocked` e `objects_after_clear` prontos para alternar visibilidade.
- `mine.objects_after_clear` é a mina; ela é **interagível**: entrar → `res://maps/caverna.tscn`.
- `npc_pantano` (do outro lado do rio) é a passagem para `res://maps/pantano.tscn`.
- Os caminhos das cenas de destino são sugestões; ajuste ao projeto. As áreas trancadas foram testadas:
  nenhuma é alcançável sem a quest certa.

## Spots

`spots[]` traz `id`, `type` (`player_start`, `npc`, `quest_npc`, `transition_npc`, `item`, `quest_item`,
`enemy_spawn`), `tile`, `pos`, `sort_y`, `note` e um `placeholder_sprite` só para teste
(os `marker_*` do kit). O jogo deve trocar o placeholder pelo NPC/inimigo/baú real.

## Árvores translúcidas

Quando um digimon passa atrás de uma árvore, a árvore vai para ~45% de opacidade (já previsto no
`MapLoader._Process` com `PlayerPath`, ou use `kit/godot/tree_fade.gd` como referência).

## Checklist para considerar pronto

- [ ] A cena no Godot, em 2x, bate com `mapa/previas/detalhes_*.png` (blocos, árvores, planaltos e rampas).
- [ ] O player não atravessa floresta, água, laterais de planalto nem bloqueios antes da quest.
- [ ] Planaltos só por rampa; o digimon sobe 18 px no planalto.
- [ ] Ao concluir cada quest, o bloqueio troca de visual e a passagem abre; a mina leva à caverna.
- [ ] Árvores ficam translúcidas quando o digimon passa atrás.

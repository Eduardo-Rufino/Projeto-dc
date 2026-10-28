# Modelo — Mapa da Caverna (para o Claude Code montar no Godot)

Este pacote segue **o mesmo formato do pacote da floresta** (`digimon-iso-map` v1), com o que a caverna acrescenta:
**o escuro com raio de visão**, luzes fixas, sprites de brilho, objetos que acendem/ligam, o abismo com a ponte de dados
e uma camada de decalques. O mapa inteiro já vem pronto no JSON; nada precisa ser posicionado à mão.

## Estrutura

```
modelo_caverna/
  GUIA_CLAUDE_CODE.md            <- este arquivo
  kit/
    manifest.json                <- todos os sprites: arquivo, quadros, tamanho, âncora, brilho, colisão
    LEIA-ME.txt
    tile_sheets/                 <- blocos (pedra, cascalho, musgo, água, patamar, paredes, escadas, abismo)
    prop_sheets/                 <- objetos (formações, mina, digital, bloqueios, bocas da caverna...)
    overlays/                    <- sobreposições de bloco (trilhos, espuma da água, ponte de dados, restos da ponte)
    glow/                        <- só os pixels que brilham de cada sprite que brilha (mesmo tamanho e quadros)
    luz/                         <- texturas das luzes (elipses em faixas pontilhadas, já no tamanho certo)
    props/                       <- ícones provisórios dos spots (debug)
  mapa/
    mapa_caverna.json            <- o mapa inteiro
    previas/                     <- como DEVE ficar: mapa antes/depois das quests, detalhes em 2x, o escuro, a planta
    godot/MapLoaderCaverna.cs    <- REFERÊNCIA (não compilada/testada) de como carregar tudo
  ferramentas/montar_previa.py   <- remonta o mapa só com o JSON + kit (Python + Pillow + numpy), para conferir
```

## Regras visuais (iguais à floresta)

- Losango 64×32 px nativos, **mundo em 2x, digimons em 3x, filtro Nearest** em tudo (inclusive nas texturas de luz).
- Projeção: `X = (gx - gy) * 32`, `Y = (gx + gy) * 16` (px nativos, chão de pedra, nível 0).
- Superfícies mais baixas que a pedra: cascalho +3 px, água +8 px. Cada nível de altura = 18 px.
  Paredes: `wall_low` 1 nível, `wall` 2, `wall_high` 3. Patamar (`ledge`) 1 nível.
- `pos` é sempre o ponto final (com desnível e altura); `sort_y` é o Y do chão, usado para ordenar.
- Paredes que ficam **na frente** de chão andável são sempre baixas (1 nível), para não esconder o digimon.

## Montando a cena (ordem das camadas)

1. Nó raiz `Node2D` com `Scale = (2,2)`, `TextureFilter = Nearest`.
2. **Ground** (sem Y-sort): `tiles` com `layer == "ground"`, na ordem do JSON. Sprite com `offset = -tile_center` em `pos`.
   As `overlays` do bloco vão por cima no mesmo `pos`, mas com `offset = -anchor` **da própria sobreposição**
   (diferente da floresta, onde usava o centro do bloco).
3. **Decals** (sem Y-sort): `objects` com `layer == "decal"` (poças, cabos, pedrinhas, restos da ponte e, depois da
   q_energia, a ponte de dados). Ficam no chão, sempre abaixo do digimon.
4. **Sorted** (Y-sort): `tiles` com `layer == "sorted"` (paredes, patamares, escadas) + todos os outros `objects`,
   os objetos dos bloqueios, NPCs, inimigos e o player. Cada item é um `Node2D` em `(pos.x, sort_y)` com o sprite em
   `offset = -anchor + (0, pos.y - sort_y)`. Empate: blocos antes de objetos, depois a ordem do JSON.
5. Animação: blocos a 5 fps, objetos a 4 fps, os dois começando no quadro `(gx + gy) % 4`.
6. Peças que precisam de atenção:
   - `timber_frame2_gx_a/_b`: escora de 2 blocos **cortada em duas metades** (uma por faixa) para o Y-sort funcionar
     quando o digimon passa por baixo. As duas já vêm no JSON.
   - Bloqueios (desmoronamento, barreira) são um sprite só de 2 blocos, como na floresta.
   - `cave_mouth_forest_gx` e `cave_exit_gy` têm a âncora no pé da parede, no meio da abertura.

A prova de que as regras estão certas: `python ferramentas/montar_previa.py antes` (ou `depois`) monta o mapa só com
estas regras e bate com `mapa/previas/mapa_antes_1x.png` / `mapa_depois_1x.png`.

## O escuro (a mecânica principal da caverna)

Tudo fica em `vision` e `lights` no JSON. A prévia faz exatamente isto (veja `mapa/previas/escuro_*.png`):

- **CanvasModulate** com `vision.ambient_color` = `[0.035, 0.03, 0.055]`. Fora de uma luz não se vê nada (nem inimigo, nem baú).
- **Luz do digimon**: um `PointLight2D` que segue o digimon (pés + 8 px para cima, mais o `VisualLift` em patamar).
  O raio depende do estado (`vision.player_light.states`):

  | estado | quando | raio | textura |
  |---|---|---|---|
  | `sem_bonus` | padrão | 2,2 blocos | `luz/luz_r2_2.png` |
  | `fogo` | o digimon que anda com o player é do elemento **fogo** | 3,6 | `luz_r3_6` |
  | `lanterna` | a q_lanterna foi concluída (item `lanterna`) | 5,5 | `luz_r5_5` |
  | `fogo_lanterna` | as duas coisas | 6,5 | `luz_r6_5` |

  Ao trocar de estado, crescer o raio em ~0,6 s (`SetVisionState` no loader faz isso).
- **Luzes fixas** (`lights`, 49 no total): cada uma vira um `PointLight2D` com `texture = luz/<texture>.png`,
  `texture_scale = 1` (a textura já tem o tamanho certo em px do mundo; o mapa em 2x amplia junto), `color`, `energy`,
  sombras desligadas. `enabled` diz quando ela existe:
  - `always` — sempre (lanternas do acampamento, cristais, cogumelos, luz do dia na saída, luz da boca da floresta);
  - `after:q_energia` — liga quando a quest termina (terminais das ruínas, postes e ponte de dados);
  - `when_on:<id>` — liga quando o jogador acende/liga aquele objeto (lanternas apagadas, caixas de junção).
  As luzes com `flicker: true` (lanternas) tremulam: energia × 1 / 0,92 / 1 / 0,86 a ~6 trocas por segundo.
- **Brilho**: chama, cristal, tela, LED e ponte de dados precisam aparecer no escuro mesmo longe da luz.
  Cada sprite que brilha tem `glow` no manifest (`glow/<nome>.png`): mesma folha e quadros, só com os pixels que
  brilham. Coloque como filho do sprite, mesmo offset e quadro, com `light_mask = 2` (só o bit 2), e crie **uma**
  `DirectionalLight2D` com `range_item_cull_mask = 2`, `energy = 1`, modo Add: ela acende só esses pixels.
  As luzes normais ficam com `range_item_cull_mask = 1`. (Se preferir, teste também um material *unshaded* no sprite
  de brilho; dependendo da versão do Godot o CanvasModulate ainda escurece itens unshaded, por isso a sugestão da luz.)
- Ideia opcional que combina: inimigos no escuro mostram só um par de olhos (1–2 px de brilho) fora do raio.

## Andar / colisão

- `walk[gy][gx]`: `.` anda · `#` não anda (rocha, água, **abismo**) · `R` escada · `Q` bloqueio de quest · `O` objeto sólido.
- `elevation` 0/1; só se passa entre blocos de mesmo nível, exceto pela escada (`ramps`), igual à floresta.
- A **ponte**: os blocos de abismo da travessia são `Q` do `bloqueio_abismo`; viram andáveis quando a q_energia termina.
- O `MapLoaderCaverna.cs` tem `CanStep`, `VisualLift`, `SetBlockerCleared`, `OnQuestCompleted`, `SwitchOn`, `SetVisionState`.

## Quests, interagíveis e bloqueios

| Quest | NPC (spot) | Precisa | Libera / efeito |
|---|---|---|---|
| `q_lanterna` | `npc_minerador` (acampamento) | `lanterna_velha` (spot `item_lanterna`, ao lado do vagonete descarrilado, no fim dos trilhos) | dá a **lanterna** (estado de visão) e libera `bloqueio_desmoronamento` (o desmoronamento vira escombros) |
| `q_energia` | `npc_tecnico` (terraço das ruínas) | ligar `juncao_1`, `juncao_2`, `juncao_3` | acende as ruínas (`after:q_energia`) e liga a **ponte de dados**: libera `bloqueio_abismo` → saída |
| `q_lanternas` (opcional) | `npc_vela` (galeria funda) | acender `lanterna_apagada_1..4` | quebra a barreira de cristal: libera `bloqueio_cristal` → gruta secreta (item raro `bau_gruta`) |

- `interactables` lista o que se usa no mapa: as 4 lanternas apagadas (**precisam da lanterna OU de um digimon de fogo**),
  as 3 caixas de junção e a lanterna velha. Objetos `kind: "switchable"` têm `sprite_after` (versão acesa/ligada) e
  `switch_on` (`interact:...` ou `quest:q_energia`).
- Cada bloqueio tem `objects_while_blocked` / `objects_after_clear`. Na ponte, o "depois" inclui os postes ligados e
  um pedaço de ponte de dados por bloco (camada Decals).
- NPCs sugeridos (troque pelos do jogo): minerador → Drimogemon; técnico → Hagurumon; vela → Candlemon.
- Ordem testada: sem quest só se alcança entrada, acampamento e os túneis dos trilhos (com as salas laterais);
  depois da q_lanterna abre a galeria, lago, ruínas e a beira do abismo; depois da q_energia, a saída.
  Todos os spots ficam alcançáveis na ordem certa, já contando os objetos sólidos.

## Transições

- `saida_floresta` (boca da caverna, oeste): volta para `res://maps/floresta.tscn`, chegando ao lado da mina.
  Na floresta, a mina leva para cá e o jogador nasce no spot `spawn`.
- `saida_proxima` (norte, depois da ponte): `res://maps/proxima_area.tscn` — o destino ainda não foi decidido.

## Spots

`spots[]`: `id`, `type` (`player_start`, `transition`, `quest_npc`, `quest_item`, `lamp_off`, `junction`, `item`,
`enemy_spawn`), `tile`, `pos`, `sort_y`, `note`, `opens_with` (que quest abre aquela área) e `placeholder_sprite`.

## Checklist

- [ ] A cena em 2x bate com `mapa/previas/detalhe_*.png` (blocos, paredes baixas na frente, trilhos, abismo).
- [ ] Fora das luzes a tela fica preta; o raio do digimon muda com fogo/lanterna como na tabela.
- [ ] Cristais, chamas, telas e a ponte de dados brilham no escuro (camada de brilho).
- [ ] O player não atravessa rocha, água, abismo, objetos sólidos nem bloqueios antes da quest.
- [ ] Cada quest troca o visual certo e abre a passagem; lanternas e caixas trocam de sprite e acendem a luz.
- [ ] As duas transições funcionam (floresta ↔ caverna, caverna → próxima área depois da ponte).

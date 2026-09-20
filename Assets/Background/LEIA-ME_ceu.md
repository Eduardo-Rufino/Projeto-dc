# Fundo do centro de treinamento — céu com nuvens

Troca o fundo cinza da view do centro por um céu com nuvens, dando a ideia de que o
centro (os hexágonos) é uma ilha flutuando. Todos os PNGs são pixel art: **filtro
Nearest** em todas as texturas (Import > Filter: off / `texture_filter = NEAREST`).

## Arquivos

| Arquivo | Tamanho | Uso |
|---|---|---|
| `sky_gradient.png` | 1×552 | Degradê contínuo, sem faixas. Esticar para cobrir a área de jogo inteira (TextureRect, `stretch_mode = SCALE`). Esta é a única textura que pode usar filtro Linear, para o degradê ficar liso se a altura da tela passar de 552px. Alternativa equivalente: um `GradientTexture2D` com as cores de `SKY` em `ceu.py`. |
| `cloud_far_1..3.png` | 45–72 × 24–27 | Nuvens distantes: pequenas, pálidas, levemente transparentes. |
| `cloud_near_1..3.png` | 105–174 × 48–60 | Nuvens próximas: maiores, com sombra lilás na base. |
| `hex_shadow.png` | 268×234 | Silhueta sólida para a sombra de cada hexágono (ver "Sombra"). |
| `hex_shadow_soft.png` | 268×234 | Mesma sombra já semitransparente, para uso direto se não quiser CanvasGroup. |
| `hex_base.png` | 252×230 | Opcional: espessura de pedra sob as arestas de baixo de cada hexágono. |

`hex_shadow*` e `hex_base` estão na **mesma escala das salas** (sala = 252×218). Se as
salas forem desenhadas com escala na view, aplicar a mesma escala nesses nós.

## Ordem das camadas (de trás para a frente)

1. `sky_gradient` — fundo fixo cobrindo a área de jogo (abaixo do HUD).
2. Nuvens distantes — atrás de tudo.
3. Nuvens próximas — também atrás das salas (passam "por baixo" do centro).
4. Sombras dos hexágonos.
5. Bases dos hexágonos (opcional).
6. Salas (como já estão hoje).

Nenhuma nuvem passa na frente das salas.

## Nuvens: movimento

Todas andam da esquerda para a direita (ou o contrário, mas todas no mesmo sentido),
em loop: ao sair da tela por um lado, reaparecem do outro com y e sprite sorteados.

| Camada | Quantidade simultânea | Velocidade | Y |
|---|---|---|---|
| Distantes | 5–7 | 4–8 px/s | qualquer altura da área de jogo |
| Próximas | 3–4 | 12–20 px/s | preferir a metade de baixo e as laterais do centro |

Sortear velocidade e posição inicial por nuvem para não andarem alinhadas. Movimento
em pixels inteiros (arredondar a posição) para não borrar o pixel art.

## Sombra dos hexágonos

Uma sombra por hexágono, na posição da sala deslocada **(-8, -8 + 26)**: a imagem tem
margem de 8px em volta e a sombra fica 26px abaixo da sala.

Para as sombras de hexágonos vizinhos não escurecerem em dobro onde se sobrepõem:
colocar todas as `hex_shadow.png` (silhueta sólida) como filhas de um único
`CanvasGroup` e dar ao grupo `self_modulate = Color(1, 1, 1, 0.3)`.
Alternativa mais simples: usar `hex_shadow_soft.png` direto (aceita leve escurecimento
extra nas sobreposições).

## Base dos hexágonos (opcional)

`hex_base.png` na **mesma posição da sala** (canto superior esquerdo igual), desenhada
logo antes dela. A parte de cima da imagem fica escondida sob a sala; aparecem só os
~12px abaixo das arestas de baixo. Com várias salas, manter a mesma ordenação por y já
usada nas salas (a base de uma sala de cima fica atrás da sala de baixo).

## Prévia

`ceu_mock_com_base.png` mostra o resultado sobre o print
atual do jogo (montagem aproximada, só para referência visual).

Gerado por `ceu.py` (requer `floresta.py` e `hexroom.py` na mesma pasta).

## Ciclo do dia (variações do céu)

Oito céus por horário, no mesmo formato de `sky_gradient.png` (degradê contínuo 1×552):

| Hora | Arquivo | Modulate das nuvens | Alpha das estrelas |
|---|---|---|---|
| 00:00 | `sky_00_noite.png` | (110, 120, 170) | 1.0 |
| 05:00 | `sky_05_madrugada.png` | (150, 146, 190) | 0.6 |
| 06:30 | `sky_06_amanhecer.png` | (255, 214, 206) | 0.0 |
| 09:00 | `sky_09_manha.png` | (255, 255, 255) | 0.0 |
| 12:00 | `sky_12_meio_dia.png` | (255, 255, 255) | 0.0 |
| 16:00 | `sky_16_tarde.png` | (255, 244, 228) | 0.0 |
| 18:00 | `sky_18_por_do_sol.png` | (255, 188, 160) | 0.0 |
| 19:30 | `sky_19_anoitecer.png` | (170, 150, 196) | 0.4 |

(`sky_12_meio_dia.png` substitui `sky_gradient.png`; os dois podem coexistir.)

Como aplicar, usando a hora do relógio do jogo:
1. Achar os dois horários da tabela que cercam a hora atual (após 19:30 o próximo é
   00:00 do dia seguinte) e o fator `t` entre eles (0 a 1).
2. Céu: duas `TextureRect` sobrepostas com os dois degradês; a de cima com
   `modulate.a = t`. Ao passar do horário, trocar as texturas.
3. Nuvens: `modulate` de todas as nuvens = interpolação das duas cores da tabela
   (valores de 0 a 255; dividir por 255 para `Color`).
4. Estrelas: `sky_stars.png` (192×192, repetível, filtro Nearest) numa `TextureRect`
   com `stretch_mode = TILE`, entre o céu e as nuvens distantes, com `modulate.a` =
   interpolação do alpha da tabela. Opcional: piscar algumas estrelas variando o alpha.

As salas não mudam de cor. Se quiser que o centro também escureça à noite, aplicar nas
salas um `modulate` leve (ex.: (200, 205, 235) à meia-noite, branco durante o dia),
interpolado da mesma forma.

Prévia de todos os horários: `ceu_ciclo_dia.png`. Gerado por `ceu_tempo.py`.

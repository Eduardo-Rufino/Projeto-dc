# Sistema de Passivas — Especificação

Documento de referência para implementação. Todos os números aqui já foram balanceados
contra as mecânicas reais do sistema de batalha (fórmula subtrativa, Taunt, dash, órbita,
fuga, reavaliação de alvo, esquiva geométrica, decaimento de cura).

**Não altere valores numéricos durante a implementação.** Se algum número parecer errado
ou impossível de implementar como descrito, pare e pergunte.

---

## 1. Modelo de dados

- Cada espécie de Digimon tem um **pool de 2 ou 3 passivas possíveis**, definido em tabela.
- Ao nascer ou evoluir, o Digimon sorteia **exatamente 1 passiva** desse pool.
- A passiva é gravada na instância e **não pode ser alterada** depois.
- A passiva precisa ser compatível com a `RoleType` fixa da espécie (e com o `SupportType`,
  no caso de Support).

### Restrição de pool por Role

Toda passiva declara a quais Roles ela se aplica. As de Support declaram também o subtipo
(`Healer`, `Buffer`, `Debuffer`, ou qualquer um).

---

## 2. Regras sistêmicas (valem para todas as passivas)

Estas regras existem por causa de propriedades específicas do sistema de combate. Violá-las
quebra o balanceamento independente dos números individuais.

### R1 — Passivas ofensivas nunca modificam % de ATK

O dano é `max(1, ATK_efetivo - DEF_efetiva)`. Numa fórmula subtrativa, +30% de ATK vale
+13% de dano contra DEF baixa e +100% contra DEF alta. Toda passiva de dano deve multiplicar
o **dano final** — depois da subtração, depois dos multiplicadores de Attribute/Element,
depois da variância e depois do crítico.

Única exceção: **Rompe-Guarda**, que existe justamente para escalar com DEF, e por isso
carrega um teto próprio.

### R2 — Cura, regeneração e escudo de passiva obedecem o decaimento do Healer

Mesma curva já existente: 100% de eficácia nos primeiros 30s, depois -15% a cada 5s, piso
de 20%. Essa trava existe para impedir luta matematicamente invencível; passiva sem
decaimento fura a trava por fora.

Aplica-se a: Fôlego, Sede de Batalha, Vigília, Mãos Firmes, Aura Vital.

### R3 — Passivas de Speed custam o dobro

`stat Speed` é double-dip: entra na cadência real de ataque pela razão
`Speed_atacante / Speed_alvo` (piso de 0.5s entre golpes). Reduzir 20% do Speed de um alvo
faz o **time inteiro** bater ~25% mais rápido nele. Orçamento: uma passiva de Speed tem
metade do percentual de uma passiva de dano equivalente.

### R4 — Separar `velocidade_movimento` de `stat_Speed`

São coisas diferentes:

- `velocidade_movimento` — o 80 / 92 / 105 definido pela Role, usado para deslocamento.
- `stat_Speed` — o stat da espécie, usado na cadência de ataque e na soma do EnemyGenerator.

Se o `PassiveData` não separar os dois campos, passivas de mobilidade vão acelerar ataques
sem querer. **Pés Leves** mexe apenas em `velocidade_movimento`. **Alvo Marcado** mexe
apenas em `stat_Speed`.

### R5 — Inimigos gerados também recebem passivas

O rebalanceamento do `EnemyGenerator` compara soma linear de stats, e passiva não é stat.
Sem passivas do lado inimigo, o time do jogador ganha um poder invisível que o gerador
nunca compensa.

- **Batalha Livre / Encontros Selvagens**: o inimigo sorteia da mesma tabela da espécie.
- **Torneios**: as passivas ficam fixas no `TournamentData`, definidas por design.

---

## 3. Hooks necessários

A maioria já mapeia para pontos existentes no código. A implementação deve **pendurar-se
nesses pontos**, não criar sistemas paralelos.

| Hook | Onde | Usado por |
|---|---|---|
| `on_damage_final_dealt` | após todo o pipeline do DamageCalculator | Fúria Crescente, Golpe Pesado, Investida, Execução, Golpe Sorrateiro, Postura Firme |
| `on_damage_final_taken` | idem, lado do alvo | Couraça, Teimosia, Última Trincheira, Espinhos, Grito de Guerra |
| `on_defense_pierce` | cálculo da DEF efetiva | Rompe-Guarda |
| `on_crit_roll` | chance e multiplicador de crítico | Precisão Letal, Fio da Lâmina |
| `on_taunt` | aplicação e cooldown do Taunt | Provocação Insistente, Grito de Guerra |
| `on_dash` | disparo, cooldown e concessão do dash | Investida, Passo Fantasma |
| `on_target_score` | pontuação `distância + penalidade_role × 70` | Infiltrador |
| `on_target_reevaluate` | timer de 3s de reavaliação | Fixação |
| `on_projectile_aim` | mira do projétil e margem de esquiva | Predição |
| `on_orbit` / `on_flee` | comportamento de órbita e fuga | Postura Firme, Pés Leves, Instinto de Fuga |
| `on_attack_landed` | acerto confirmado | Fúria Crescente, Golpe Pesado, Fixação, Alvo Marcado |
| `on_heal` | cura e escudo | Mãos Firmes, Vigília |
| `on_buff_apply` | pilhas de buff/debuff | Ressonância, Dupla Voz, Marca Dupla |
| `on_tick` | por segundo | Fôlego, Aura Vital |

---

## 4. Catálogo de passivas

31 passivas. O comentário em itálico explica a razão do número — útil para não
"consertar" algo que já foi ajustado de propósito.

### Tank (6)

| Nome | Efeito |
|---|---|
| **Couraça** | -18% no dano final recebido. *Referência de orçamento defensivo do jogo.* |
| **Grito de Guerra** | Inimigos sob Taunt causam -30% de dano durante os 4s. *Taunt tem ~50% de uptime, então o efetivo é ~-15%, empatando com Couraça.* |
| **Provocação Insistente** | Taunt a cada 6.5s em vez de 8s. *Uptime vai a 62%. Não descer disso sem playtest.* |
| **Espinhos** | Devolve 15% do dano final recebido em melee. *Dano refletido NÃO dispara Espinhos do outro lado — senão dois Tanks entram em loop infinito.* |
| **Última Trincheira** | Abaixo de 40% de HP, -50% no dano final por 5s, uma vez por luta. *40% e não 30%: com burst de Assassin, o golpe que cruza 30% costuma ser o que mata.* |
| **Fôlego** | Regenera 0.6% do HP máximo por segundo, com decaimento (R2). *Valor baixo de propósito: o HP persiste entre batalhas, então o valor real dela está fora da luta.* |

### Warrior (6)

| Nome | Efeito |
|---|---|
| **Investida** | Cooldown do dash 6s → 4s **e** o primeiro golpe após um dash causa +60% de dano final. *As duas coisas juntas numa passiva só: numa arena de 3 hexágonos o Warrior já nasce em melee e o dash quase não dispara, então cada metade sozinha seria fraca demais.* |
| **Golpe Pesado** | A cada 4º ataque, +50% de dano final em área pequena ao redor do alvo. |
| **Fúria Crescente** | +5% de dano final por golpe acertado, até 5 pilhas, decai após 3s sem atacar. |
| **Sede de Batalha** | Cura 20% do dano final causado, com decaimento (R2). *Escala junto com o multiplicador de tipo: em vantagem dupla (x2.0) a cura dobra.* |
| **Teimosia** | Redução de dano final escala com o HP perdido, até -30% no HP mínimo. |
| **Rompe-Guarda** | Ignora 25% da DEF do alvo, **com o ganho limitado a +40% do dano base**. *O teto é obrigatório: sem ele, contra um inimigo no teto de defesa de 85%, ignorar DEF resultava em 2.7x de dano.* |

### Assassin (6)

| Nome | Efeito |
|---|---|
| **Precisão Letal** | Crítico 10% → 22%. *Baseline de orçamento ofensivo: +10.9% de dano médio.* |
| **Fio da Lâmina** | Crítico causa x3.2 em vez de x2. *Calibrado para empatar com Precisão Letal em dano médio (1.22x esperado). A escolha entre as duas é consistência vs. variância.* |
| **Golpe Sorrateiro** | +80% de dano final no primeiro golpe contra cada alvo, **uma vez por alvo por luta**. *O limite por alvo é essencial — sem ele, a reavaliação de 3s vira farm de aberturas.* |
| **Execução** | +25% de dano final contra alvos abaixo de 40% de HP. |
| **Infiltrador** | Penalidade de Role na pontuação de alvo passa de ×70 para ×130. *Na prática é um lock, não uma prioridade: nenhuma distância na arena alcança 520 de score, então ele ignora Tank/Warrior até o backline morrer. É a intenção.* |
| **Passo Fantasma** | Ganha o dash de melee (cooldown 4s, não 6s). *Cooldown menor porque o Assassin já é speed 105; o valor da passiva é só fechar o gap inicial.* |

### Ranged (6)

| Nome | Efeito |
|---|---|
| **Postura Firme** | Desliga a órbita **e a fuga**; +25% de dano final. *Desligar só a órbita teria custo quase zero. Sem fuga, ele leva dash de Warrior na cara — aí o tradeoff é real.* |
| **Pés Leves** | Raio de fuga 130 → 190 e +8% de `velocidade_movimento` ao fugir (R4). *+8% leva a ~99: o Assassin (105) ainda pega, o Warrior (80) sofre. Não subir — a 15% ele fica inalcançável por qualquer Role.* |
| **Fixação** | Não reavalia alvo (troca só quando o alvo morre) e cada acerto consecutivo reduz 0.06s do cooldown, até -0.24s. *A rigidez é menos custosa do que parece: ele trava justamente no Support inimigo, que é o alvo que a pontuação já elege.* |
| **Alvo Marcado** | A cada 4º acerto, -12% de `stat_Speed` no alvo por 5s (R3, R4). *Empilha com o Debuffer sem conflito — o Debuffer mexe em ATK, esta mexe em Speed.* |
| **Projétil Perfurante** | Atravessa e atinge um segundo inimigo por 50% do dano. |
| **Predição** | O projétil mira a posição prevista do alvo, reduzindo pela metade a margem de esquiva geométrica. *Único número da lista que não dá para orçar no papel — depende da taxa de esquiva real. Ver seção 5.* |

### Support (7) — pool chaveado por `(Role, SupportType)`

| Nome | Subtipo | Efeito |
|---|---|---|
| **Vigília** | Healer | Sem ninguém ferido, aplica escudo de 8% do HP máximo no aliado sem escudo. Um escudo por aliado, não renova até ser consumido, obedece R2. *Preenche o tempo morto em que o Healer fica parado.* |
| **Mãos Firmes** | Healer | Cura 15% → 19% do HP máximo, curva de decaimento original inalterada. *Não antecipar o decaimento como compensação: boa parte das lutas 3v3 acaba antes dos 30s, então o "custo" nunca seria pago.* |
| **Ressonância** | Buffer | Empilha até 3x, mas a 3ª pilha vale +10% em vez de +20%. *O buff do Buffer é % de ATK e sofre de R1: a 2ª pilha já multiplica o dano por ~2.3 contra DEF alta.* |
| **Dupla Voz** | Buffer | Buffa os dois maiores atacantes aliados com +12% cada, em vez de um com +20%. |
| **Marca Dupla** | Debuffer | Debuffa os dois maiores atacantes inimigos com -14% cada. |
| **Instinto de Fuga** | Healer / Buffer | Ganha o kiting que hoje só o Debuffer tem. *A fuga deve respeitar o alcance (150) do alvo aliado — senão vira um Healer que sobrevive sem curar.* |
| **Aura Vital** | Qualquer | Regeneração passiva num raio, com decaimento (R2). |

---

## 5. Pontos abertos (não resolver na implementação)

Deixar como está e instrumentar para playtest:

1. **Predição** — instrumentar telemetria de taxa de esquiva por Role antes de tunar.
   Contra um Warrior lento pode valer quase nada; contra um Assassin de 105, +30% de DPS efetivo.
2. **Golpe Sorrateiro vs. Precisão Letal** — empatam na média, mas dano front-loaded vale
   mais na prática. Se dominar o pool em playtest, cortar para +60%.
3. **Provocação Insistente** — se 62% de uptime ainda anular o posicionamento inimigo, subir para 7s.
4. **Rompe-Guarda vs. teto de defesa de 85%** — as duas mecânicas atacam o mesmo problema
   por lados opostos. Se a defesa virar stat inútil, afrouxar o teto de 85%, não a passiva.

---

## 6. Se um dia forem 2 passivas ativas simultâneas

O balanceamento acima assume **1 passiva ativa**. Combinações a bloquear na tabela caso o
limite mude:

- Infiltrador + Passo Fantasma
- Postura Firme + Fixação
- Couraça + Fôlego
- Qualquer par com duas fontes de cura/regen no mesmo Digimon

---

## 7. Combate 1x1

Passivas de time (Grito de Guerra, Dupla Voz, Marca Dupla, Aura Vital, Vigília, e o Taunt
em geral) têm valor reduzido ou nulo nos Encontros Selvagens 1x1. **Isso é aceitável e não
precisa de tratamento especial** — o jogador simplesmente não leva esse Digimon para 1x1.
Não criar lógica de auto-aplicação de aura em time de 1 membro.
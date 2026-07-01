# Copilot Instructions

## Diretrizes de projeto
- Não altere a lógica de gameplay (batalha e treino) sem confirmar com o desenvolvedor; preferir mudanças que mantenham comportamento existente e evitar 'gambiarras'.
- Ao ajustar lógica de jogo, usar instâncias do Center para batalhas (não criar instâncias separadas), oferecer overloads para definir Digimon por instância, e evitar conexões de evento duplicadas.
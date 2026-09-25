# Pacote: revisão dos requisitos de evolução

1. Copie esta pasta para a raiz do projeto (ou para qualquer lugar que o Claude do terminal enxergue).
2. Cole no Claude do terminal: "Leia PROMPT_CLAUDE_TERMINAL.md desta pasta e execute a Fase 1."
3. Ele vai parar ao fim de cada fase. Só autorize a fase seguinte se o relatório bater com o esperado.

Arquivos:
- PROMPT_CLAUDE_TERMINAL.md — as instruções, em fases, com critérios de aceite.
- requisitos_evolucao_revisados.csv — valores finais das 337 ligações.
- analise/verificar.py — mede o problema a partir do REQUISITOS_EVOLUCAO.md (Python 3, sem dependências).
- analise/esperado_dados_atuais.txt / esperado_com_csv.txt — o que o verificador deve imprimir antes e depois.

## v4 (Fase 3 v3)
- PROMPT_FASE3_V3.md — correção do achado Gaogamon→MachGaogamon e dos 5 pais com o mesmo defeito.
- verificar.py ganhou as checagens F (inalcançável por alguma rota de entrada) e G (informativo: carry-over).
- CSV v4: 18 ligações diferentes do v3.

## v5 (Fase 5 — fechamento)
- PROMPT_FINAL.md — conserto do par Growlmon (EA 60 no MetalGreymon), consulta das rotas medidas e merge na development.
- CSV v5: 1 ligação diferente do v4.

## v6 (Fase 6)
- PROMPT_FASE6.md — Baby/InTraining passam a assinar por EA/ED (stats que o nível não dá), mais o arrasto nos estágios seguintes.
- verificar.py ganhou a checagem H (rival de Score maior que fica válido só subindo de nível).
- CSV v6: 189 ligações diferentes do v5.

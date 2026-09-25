"""importar_csv.py - Fase 3 da revisao dos requisitos de evolucao.

Le revisao_evolucao/requisitos_evolucao_revisados.csv e grava os valores finais em
Data/Evolutions/*.json (a fonte que o jogo realmente le - ver DatabaseManager.LoadEvolutions).

So mexe nas ligacoes marcadas "alterada=sim" no CSV. Cada objeto tocado e reescrito por
inteiro, substituindo o bloco "{...}" original pelo novo, texto puro (sem json.dump do
arquivo inteiro) - assim nenhum outro objeto do mesmo arquivo muda de formatacao. Campos
opcionais (requiredBattles/requiredWinRate/min-maxDiscipline/min-maxHappiness) so aparecem
quando o valor final foge do padrao, na ordem declarada em Scripts/Data/EvolutionData.cs
(RequiredBattles, RequiredWinRate, MinDiscipline, MaxDiscipline, MinHappiness, MaxHappiness).
requiredWinRate e gravado como fracao (Vitorias% / 100). Mantem LF, sem BOM (os arquivos ja
sao assim - ver Fase 1, item 10/11).

Uso:
  python3 importar_csv.py                  # aplica de verdade
  python3 importar_csv.py --dry-run         # so mostra o que mudaria, nao grava nada
"""
import argparse
import csv
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CSV_PATH = os.path.join(ROOT, "revisao_evolucao", "requisitos_evolucao_revisados.csv")
EVO_DIR = os.path.join(ROOT, "Data", "Evolutions")
REPORT_PATH = os.path.join(ROOT, "revisao_evolucao", "analise", "relatorio_importacao.txt")

STAT_KEYS = [
    ("HP", "healthPoints"),
    ("PA", "physicalDamage"),
    ("PD", "physicalDefense"),
    ("EA", "specialDamage"),
    ("ED", "specialDefense"),
    ("Vel", "speed"),
]

# (coluna do CSV, campo json, valor padrao, conversor)
OPTIONAL_FIELDS = [
    ("Batalhas", "requiredBattles", 0, int),
    ("Vitorias%", "requiredWinRate", 0.0, lambda v: float(v) / 100.0),
    ("Disc_min", "minDiscipline", 0, int),
    ("Disc_max", "maxDiscipline", 100, int),
    ("Felic_min", "minHappiness", 0, int),
    ("Felic_max", "maxHappiness", 100, int),
]


def load_name_to_id():
    name_to_id = {}
    for path in glob.glob(os.path.join(ROOT, "Data", "Digimon", "*.json")):
        data = json.load(open(path, encoding="utf-8-sig"))
        name_to_id[data["name"]] = data["id"]
    return name_to_id


def fmt_num(value):
    """Formata um numero exatamente como o restante dos arquivos (int sem .0; float sem
    zeros a mais - 1.85 continua 1.85, 1.9 continua 1.9, nunca 1.8999999999999997)."""
    if isinstance(value, int):
        return str(value)
    text = f"{value:.10f}".rstrip("0").rstrip(".")
    return text if text else "0"


def build_object_text(row, indent):
    """Monta o texto de um objeto de evolucao (sem chave/virgula externas), na ordem
    canonica, no mesmo estilo de indentacao (2 espacos por nivel) dos arquivos existentes."""
    pad1 = " " * (indent + 2)
    pad2 = " " * (indent + 4)

    lines = ["{"]
    lines.append(f'{pad1}"fromDigimonId": {row["_from_id"]},')
    lines.append(f'{pad1}"toDigimonId": {row["_to_id"]},')
    lines.append(f'{pad1}"requiredLevel": {int(row["Lv"])},')
    lines.append(f'{pad1}"requiredAgeInDays": {int(row["Idade"])},')
    lines.append(f'{pad1}"statMultiplier": {fmt_num(float(row["xMult"]))},')
    lines.append(f'{pad1}"requiredStats": {{')
    for i, (csv_col, json_key) in enumerate(STAT_KEYS):
        comma = "," if i < len(STAT_KEYS) - 1 else ""
        lines.append(f'{pad2}"{json_key}": {int(row[csv_col])}{comma}')
    lines.append(f"{pad1}}}")

    optional_lines = []
    for csv_col, json_key, default, conv in OPTIONAL_FIELDS:
        value = conv(row[csv_col])
        if value == default:
            continue
        optional_lines.append(f'{pad1}"{json_key}": {fmt_num(value)}')

    if optional_lines:
        lines[-1] += ","
        lines.extend(l + ("," if idx < len(optional_lines) - 1 else "") for idx, l in enumerate(optional_lines))

    lines.append(" " * indent + "}")
    return "\n".join(lines)


def split_top_level_objects(array_inner):
    """Divide o miolo de um array JSON (entre [ e ]) nos objetos de nivel 0, respeitando
    chaves aninhadas (requiredStats). Devolve lista de (start, end) no texto original."""
    spans = []
    depth = 0
    start = None
    for i, ch in enumerate(array_inner):
        if ch == "{":
            if depth == 0:
                start = i
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                spans.append((start, i + 1))
    return spans


def process_file(path, rows_by_pair, dry_run, report):
    raw = open(path, "rb").read()
    assert not raw.startswith(b"\xef\xbb\xbf"), f"{path}: tinha BOM (inesperado)"
    text = raw.decode("utf-8")
    assert "\r\n" not in text, f"{path}: tinha CRLF (inesperado)"

    array_start = text.index("[")
    array_end = text.rindex("]")
    inner = text[array_start + 1 : array_end]

    spans = split_top_level_objects(inner)
    touched = 0
    new_inner = inner
    offset = 0

    for start, end in spans:
        block = inner[start:end]
        m = re.search(r'"fromDigimonId":\s*(\d+),\s*"toDigimonId":\s*(\d+)', block)
        if not m:
            continue
        key = (int(m.group(1)), int(m.group(2)))
        row = rows_by_pair.get(key)
        if row is None:
            continue

        indent = len(inner[:start]) - len(inner[:start].rstrip(" \t").rstrip("\n").rsplit("\n", 1)[-1]) if False else None
        # indentacao do objeto = espacos entre o ultimo '\n' antes de start e o '{'
        line_start = inner.rfind("\n", 0, start) + 1
        indent = start - line_start

        new_block = build_object_text(row, indent)

        if new_block != block:
            new_inner = new_inner[: start + offset] + new_block + new_inner[end + offset :]
            offset += len(new_block) - len(block)
            touched += 1
            report.append(
                f"  {row['origem']} -> {row['destino']}  ({os.path.basename(path)})\n"
                f"    antes: {' '.join(block.split())}\n"
                f"    depois: {' '.join(new_block.split())}\n"
            )

    if touched == 0:
        return 0

    new_text = text[: array_start + 1] + new_inner + text[array_end:]

    if not dry_run:
        with open(path, "wb") as f:
            f.write(new_text.encode("utf-8"))

    return touched


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    name_to_id = load_name_to_id()

    rows_by_pair = {}
    skipped_unchanged = 0
    with open(CSV_PATH, encoding="utf-8") as f:
        for row in csv.DictReader(f):
            if row["alterada"].strip().lower() != "sim":
                skipped_unchanged += 1
                continue

            if row["origem"] not in name_to_id:
                print(f"ERRO: origem desconhecida no CSV: {row['origem']!r}", file=sys.stderr)
                sys.exit(1)
            if row["destino"] not in name_to_id:
                print(f"ERRO: destino desconhecido no CSV: {row['destino']!r}", file=sys.stderr)
                sys.exit(1)

            row["_from_id"] = name_to_id[row["origem"]]
            row["_to_id"] = name_to_id[row["destino"]]
            key = (row["_from_id"], row["_to_id"])

            if key in rows_by_pair:
                print(f"ERRO: ligacao duplicada no CSV: {row['origem']} -> {row['destino']}", file=sys.stderr)
                sys.exit(1)

            rows_by_pair[key] = row

    matched_pairs = set()
    report = []
    files_touched = 0
    links_touched = 0

    for path in sorted(glob.glob(os.path.join(EVO_DIR, "*.json"))):
        # descobre quais pares deste arquivo existem de fato, pra cruzar com o CSV depois
        raw = open(path, encoding="utf-8-sig").read()
        for m in re.finditer(r'"fromDigimonId":\s*(\d+),\s*"toDigimonId":\s*(\d+)', raw):
            pair = (int(m.group(1)), int(m.group(2)))
            if pair in rows_by_pair:
                matched_pairs.add(pair)

        touched = process_file(path, rows_by_pair, args.dry_run, report)
        if touched:
            files_touched += 1
            links_touched += touched

    missing = set(rows_by_pair.keys()) - matched_pairs
    if missing:
        names = [f"{rows_by_pair[p]['origem']} -> {rows_by_pair[p]['destino']}" for p in missing]
        print("ERRO: ligacao(oes) do CSV nao encontrada(s) nos dados atuais (nao crio ligacao nova):", file=sys.stderr)
        for n in names:
            print(f"  - {n}", file=sys.stderr)
        sys.exit(1)

    summary = (
        f"Importacao {'(dry-run) ' if args.dry_run else ''}concluida.\n"
        f"CSV: {len(rows_by_pair) + skipped_unchanged} ligacoes ({len(rows_by_pair)} marcadas 'sim', {skipped_unchanged} 'nao').\n"
        f"Arquivos tocados: {files_touched}\n"
        f"Ligacoes com bloco JSON reescrito: {links_touched}\n"
        f"Ligacoes marcadas 'sim' cujo texto ja era identico ao final (nao regravadas): {len(rows_by_pair) - links_touched}\n"
    )
    print(summary)

    with open(REPORT_PATH, "w", encoding="utf-8") as f:
        f.write(summary + "\n" + "".join(report))

    print(f"Relatorio completo em {REPORT_PATH}")


if __name__ == "__main__":
    main()

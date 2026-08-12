#!/usr/bin/env bash
# EVAl — Valida se a tarefa "indexação do AGENTS.md em docs/ + tutor atualizado" está concluída.
# Uso: bash docs/eval.sh
# Exit 0 = tarefa completa | Exit 1 = faltam itens (loop deve continuar)

set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
FAIL=0

FILES=(
  "docs/architecture/system-design.md"
  "docs/architecture/event-driven.md"
  "docs/backend/dotnet-core-banking.md"
  "docs/backend/java-analytics-ai.md"
  "docs/frontend/angular-spa.md"
  "docs/testing/testing-guide.md"
  "docs/workflow/development-standards.md"
)

echo "== EVAl: indexacao AGENTS.md em docs/ + tutor.md =="

for f in "${FILES[@]}"; do
  if [ ! -s "$ROOT/$f" ]; then
    echo "  [FAIL] arquivo ausente ou vazio: $f"
    FAIL=1
  else
    echo "  [OK]   $f ($(wc -l < "$ROOT/$f") linhas)"
  fi
done

declare -A REQUIRED
REQUIRED[docs/architecture/system-design.md]="System Design|API Gateway|OpenTelemetry"
REQUIRED[docs/architecture/event-driven.md]="RabbitMQ|TransferCompleted|Event-Driven"
REQUIRED[docs/backend/dotnet-core-banking.md]="DDD|EF Core|HasMaxLength|Guid"
REQUIRED[docs/backend/java-analytics-ai.md]="Spring Boot|Redis|Gemini"
REQUIRED[docs/frontend/angular-spa.md]="Angular 18|SignalR|Signals"
REQUIRED[docs/testing/testing-guide.md]="xUnit|JUnit|Playwright|80%"
REQUIRED[docs/workflow/development-standards.md]="Conventional Commits|ADR|Indexação"

for f in "${!REQUIRED[@]}"; do
  PATTERN="${REQUIRED[$f]}"
  if grep -qE "$PATTERN" "$ROOT/$f" 2>/dev/null; then
    echo "  [OK]   conteudo essencial presente em $f"
  else
    echo "  [FAIL] $f nao contem padrao esperado: $PATTERN"
    FAIL=1
  fi
done

TUTOR="$ROOT/.opencode/agents/tutor.md"
if [ -f "$TUTOR" ] && grep -q "Base de Conhecimento Indexada" "$TUTOR"; then
  echo "  [OK]   tutor.md contem indice de base de conhecimento"
else
  echo "  [FAIL] tutor.md nao foi atualizado com o indice"
  FAIL=1
fi

AGENTS="$ROOT/AGENTS.md"
if [ -f "$AGENTS" ] && grep -q "Base de Conhecimento Indexada (KB)" "$AGENTS" && grep -q "docs/eval.sh" "$AGENTS"; then
  echo "  [OK]   AGENTS.md consolida o KB indexado em docs/"
else
  echo "  [FAIL] AGENTS.md nao consolida o KB indexado"
  FAIL=1
fi

if [ -f "$TUTOR" ]; then
  for ref in system-design event-driven dotnet-core-banking java-analytics-ai angular-spa testing-guide development-standards; do
    if ! find "$ROOT/docs" -name "$ref.md" -print -quit | grep -q .; then
      echo "  [FAIL] doc referenciado no tutor ausente: $ref.md"
      FAIL=1
    fi
  done
  echo "  [OK]   todos os docs referenciados no tutor existem"
fi

MD_COUNT=$(find "$ROOT/docs" -maxdepth 2 -type d \( -name architecture -o -name backend -o -name frontend -o -name testing -o -name workflow \) -exec sh -c 'ls "$1"/*.md 2>/dev/null | wc -l' _ {} \; | awk '{s+=$1} END {print s+0}')
echo "  [INFO] total de .md criados nas especializacoes: $MD_COUNT"

if [ "$FAIL" -eq 0 ]; then
  echo "== RESULTADO: PASS tarefa concluida =="
  exit 0
else
  echo "== RESULTADO: FAIL faltam itens, repetir loop =="
  exit 1
fi

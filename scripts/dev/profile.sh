# CashFlow Pro — dev helpers for Debian (bash)
# Windows counterpart: scripts/dev/profile.ps1 (same function names)
# Docs: scripts/dev/README.md
#
# Load it from ~/.bashrc:
#   source "$HOME/repos/CashFlowPro/scripts/dev/profile.sh"

# Repo root: CASHFLOW_ROOT wins, otherwise derive it from this file's location (scripts/dev/..).
CF_ROOT="${CASHFLOW_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
CF_ANALYTICS="$CF_ROOT/src/Analytics"
CF_COREBANKING="$CF_ROOT/src/CoreBanking"

# ─── Navigation ──────────────────────────────────────────
cf-root() { cd "$CF_ROOT" || return; }
cf-an()   { cd "$CF_ANALYTICS" || return; }
cf-cb()   { cd "$CF_COREBANKING" || return; }

# ─── Infrastructure (Docker Compose) ─────────────────────
# Usage: cf-up         -> postgres, redis, rabbitmq only
#        cf-up all     -> everything, including core-banking and analytics
cf-up() {
  if [ "$1" = "all" ]; then
    (cd "$CF_ROOT" && docker compose up -d --build)
  else
    (cd "$CF_ROOT" && docker compose up -d postgres redis rabbitmq)
  fi
}
cf-down() { (cd "$CF_ROOT" && docker compose down); }
cf-ps()   { (cd "$CF_ROOT" && docker compose ps); }
# Run any docker compose command from the repo root, whatever the current folder.
# Usage: cf-dc ps | cf-dc up -d analytics | cf-dc config --quiet | cf-dc restart redis
cf-dc() { (cd "$CF_ROOT" && docker compose "$@"); }
# Usage: cf-logs [service]   (default: analytics)
cf-logs() { (cd "$CF_ROOT" && docker compose logs -f --tail 100 "${1:-analytics}"); }

# ─── Analytics (Java / Spring Boot) ──────────────────────
# Needs the exec bit: chmod +x src/Analytics/mvnw
cf-an-run()   { (cd "$CF_ANALYTICS" && ./mvnw spring-boot:run); }
cf-an-build() { (cd "$CF_ANALYTICS" && ./mvnw clean package -DskipTests); }
# Usage: cf-an-test                      -> all tests
#        cf-an-test TransferEventTest    -> a single test class
# Failures are printed to the console (expected vs actual, line of the test), not hidden in target/surefire-reports.
cf-an-test() {
  local args=(test -B -Dsurefire.useFile=false -DtrimStackTrace=true)
  [ -n "$1" ] && args+=("-Dtest=$1")
  # Stop printing at "Total time:", which drops Maven's long trailing help text (the cause is printed above it).
  (cd "$CF_ANALYTICS" && ./mvnw "${args[@]}" | awk '
    /Total time:/ { exit }
    { print }
    /BUILD SUCCESS/ { passed = 1 }
    # Surefire failure line, e.g. "[ERROR]   TransferEventTest.myTest:39 » message"
    /^\[ERROR\][ ][ ]+[^ ]+\.[^ ]+:[0-9]+/ { sub(/^\[ERROR\][ ]+/, ""); failed[n++] = substr($0, 1, 160) }
    END {
      print ""
      if (n > 0) { printf "=== FAILED TESTS (%d) ===\n", n; for (i = 0; i < n; i++) print " x " failed[i] }
      else if (passed) print "=== ALL TESTS PASSED ==="
      else print "=== BUILD FAILED before the tests ran (compile error?) - read the output above ==="
    }')
}

# ─── Core Banking (.NET) ─────────────────────────────────
cf-cb-run()   { (cd "$CF_COREBANKING" && dotnet run); }
cf-cb-build() { (cd "$CF_ROOT" && dotnet build); }
cf-cb-test()  { (cd "$CF_ROOT" && dotnet test); }
cf-test-all() { cf-cb-test && cf-an-test; }

# ─── Tools & checks ──────────────────────────────────────
# Usage: cf-redis            -> interactive redis-cli
#        cf-redis KEYS '*'   -> run one command
cf-redis() { docker exec -it cashflow-redis redis-cli "$@"; }
cf-psql()  { docker exec -it cashflow-postgres psql -U cashflow -d cashflow_core; }
cf-rabbit-ui() { xdg-open http://localhost:15672; }   # user/pass: cashflow / cashflow_pass
cf-swagger()   { xdg-open http://localhost:5000/swagger; }
# Ports used by the project: 5000 core-banking, 5001 analytics, 5432, 6379, 5672, 15672, 8080
cf-ports() { ss -ltn | grep -E ':(5000|5001|5432|6379|5672|15672|8080)\b'; }

# ─── Conventions ─────────────────────────────────────────
# Create a Java package in main AND test of Analytics (en-US, lowercase, dot separated).
# Usage: cf-mkpkg model            -> com.cashflow.analytics.model
#        cf-mkpkg service.impl     -> com.cashflow.analytics.service.impl
cf-mkpkg() {
  [ -z "$1" ] && { echo "usage: cf-mkpkg <package>" >&2; return 1; }
  local rel="com/cashflow/analytics/${1//.//}" set
  for set in main test; do
    mkdir -p "$CF_ANALYTICS/src/$set/java/$rel" && echo "ok  $CF_ANALYTICS/src/$set/java/$rel"   # -p creates parents, idempotent
  done
}

# Show current branch and whether it follows Conventional Commits naming (feature/, fix/, chore/, docs/).
cf-branch() {
  local b
  b="$(cd "$CF_ROOT" && git branch --show-current)"
  echo "branch: $b"
  [[ "$b" =~ ^(feature|fix|chore|docs)/ ]] || echo "warning: branch name does not follow <type>/<name>" >&2
}

# Commit following Conventional Commits.
# Usage: cf-commit fix "align TransferCompleted JSON contract" analytics
cf-commit() {
  local type="$1" msg="$2" scope="$3" prefix
  case "$type" in feat|fix|chore|docs|test|refactor) ;; *) echo "usage: cf-commit <feat|fix|chore|docs|test|refactor> <message> [scope]" >&2; return 1;; esac
  [ -z "$msg" ] && { echo "message is required" >&2; return 1; }
  prefix="$type"; [ -n "$scope" ] && prefix="$type($scope)"
  (cd "$CF_ROOT" && git commit -m "$prefix: $msg")
}

# ─── Help ────────────────────────────────────────────────
cf-help() {
  declare -F | awk '{print $3}' | grep '^cf-' | sort
  echo; echo "Details: $CF_ROOT/scripts/dev/README.md"
}

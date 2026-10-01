# 🛠️ Dev Profile — CashFlow Pro

Funções de apoio ao desenvolvimento, com o **mesmo nome nos dois sistemas**:

| Sistema | Arquivo | Shell |
|---------|---------|-------|
| Windows 11 | [`profile.ps1`](profile.ps1) | PowerShell 5.1 / 7+ |
| Debian | [`profile.sh`](profile.sh) | bash |

Os comandos refletem a documentação: [`AGENTS.md`](../../AGENTS.md), [`docs/testing/testing-guide.md`](../../docs/testing/testing-guide.md), [`docs/backend/java-analytics-ai.md`](../../docs/backend/java-analytics-ai.md) e [`docs/backend/dotnet-core-banking.md`](../../docs/backend/dotnet-core-banking.md).

## Instalação

**Windows 11** — abra o perfil com `code $PROFILE` e adicione:
```powershell
. "C:\Users\rafae\Documents\repos\CashFlowPro\scripts\dev\profile.ps1"
```
Se aparecer erro de política de execução, rode uma vez: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.

**Debian** — adicione ao `~/.bashrc`:
```bash
source "$HOME/repos/CashFlowPro/scripts/dev/profile.sh"
```
E garanta o bit de execução do Maven Wrapper: `chmod +x src/Analytics/mvnw`.

Para um clone fora do padrão, defina `CASHFLOW_ROOT` antes de carregar o arquivo. Sem ele, a raiz é deduzida do caminho do próprio script.

Recarregar sem abrir novo terminal: `. $PROFILE` (Windows) · `source ~/.bashrc` (Debian).

## Comandos

| Comando | O que faz |
|---------|-----------|
| `cf-help` | Lista todos os comandos `cf-*` |
| `cf-root` · `cf-an` · `cf-cb` | Vai para raiz, Analytics, Core Banking |
| `cf-up` / `cf-up -All` (Win) · `cf-up all` (Debian) | Sobe infra (postgres, redis, rabbitmq) ou tudo |
| `cf-down` · `cf-ps` · `cf-logs [serviço]` | Derruba, lista, segue logs (padrão `analytics`) |
| `cf-dc <args>` | Qualquer comando `docker compose` a partir da raiz (`cf-dc up -d analytics`, `cf-dc config --quiet`, `cf-dc restart redis`) |
| `cf-an-run` · `cf-an-build` | Roda / empacota o Analytics |
| `cf-an-test [Classe]` | Todos os testes Java ou uma classe (`cf-an-test TransferEventTest`) |
| `cf-cb-run` · `cf-cb-build` · `cf-cb-test` | Core Banking (.NET) |
| `cf-test-all` | `dotnet test` + testes do Analytics |
| `cf-redis [cmd]` | `redis-cli` no container (`cf-redis KEYS 'insights:*'`) |
| `cf-psql` | `psql` no container do PostgreSQL |
| `cf-rabbit-ui` · `cf-swagger` | Abre RabbitMQ Management (`:15672`) / Swagger (`:5000`) |
| `cf-ports` | Mostra quais portas do projeto estão em uso |
| `cf-mkpkg <pacote>` | Cria o pacote Java em `main` **e** `test` (`cf-mkpkg service.impl`) |
| `cf-branch` | Mostra a branch e avisa se foge de `feature/`, `fix/`, `chore/`, `docs/` |
| `cf-commit <tipo> "<msg>" [escopo]` | Commit Conventional Commits (`cf-commit fix "align JSON" analytics`) |

> `cf-commit` não adiciona os arquivos (`git add` continua manual) e não inclui a linha `Co-Authored-By`.

## Atalhos no Cursor

As funções acima têm equivalentes em tasks do Cursor (`.vscode/tasks.json`, local — a pasta está no `.gitignore`) e em atalhos de teclado ([`cursor-keybindings.json`](cursor-keybindings.json)). Para ativar os atalhos: `Ctrl+Shift+P` → *Preferences: Open Keyboard Shortcuts (JSON)* e cole as entradas dentro do array.

| Atalho | Task (`Ctrl+Shift+P` → *Tasks: Run Task*) | Função equivalente |
|--------|-------------------------------------------|--------------------|
| `Ctrl+Alt+U` | CF: Infra up | `cf-up` |
| `Ctrl+Alt+D` | CF: Infra down | `cf-down` |
| `Ctrl+Alt+L` | CF: Logs analytics | `cf-logs` |
| `Ctrl+Alt+R` | CF: Analytics run | `cf-an-run` |
| `Ctrl+Alt+T` | CF: Analytics test (current file) | `cf-an-test <Classe do arquivo aberto>` |
| `Ctrl+Alt+Shift+T` | CF: Analytics test (all) | `cf-an-test` |
| `Ctrl+Alt+B` | CF: Analytics build | `cf-an-build` |
| `Ctrl+Alt+C` | CF: CoreBanking run | `cf-cb-run` |
| `Ctrl+Alt+A` | CF: Test all | `cf-test-all` |

## Convenções por sistema

| Tarefa | Windows (PowerShell) | Debian (bash) |
|--------|----------------------|---------------|
| Criar pasta (com pais) | `New-Item -ItemType Directory -Force -Path a\b\c` | `mkdir -p a/b/c` |
| Criar arquivo vazio | `New-Item -ItemType File -Path x.txt` (⚠️ nunca `-Force` em arquivo existente: apaga o conteúdo) | `touch x.txt` |
| Listar | `Get-ChildItem` (`ls`) | `ls -la` |
| Ler arquivo | `Get-Content x` (`cat`) | `cat x` |
| Procurar texto | `Select-String -Path *.java -Pattern "texto"` | `grep -rn "texto" .` |
| Variável de ambiente (sessão) | `$env:REDIS_HOST = "localhost"` | `export REDIS_HOST=localhost` |
| Separador de caminho | `\` | `/` |
| Maven Wrapper | `.\mvnw.cmd` | `./mvnw` (precisa de `chmod +x`) |
| Abrir URL | `Start-Process <url>` | `xdg-open <url>` |
| Encadear comandos | `a; if ($?) { b }` (PS 5.1 não tem `&&`) | `a && b` |
| Portas em uso | `Get-NetTCPConnection -State Listen` | `ss -ltn` |
| Fim de linha | CRLF no editor; o git normaliza | LF |

### Convenções do projeto (valem nos dois)
- **Código em en-US**; conversa e `docs/` em pt-BR ([`CLAUDE.md`](../../CLAUDE.md)).
- Commits: Conventional Commits (`feat:`, `fix:`, `chore:`); branch `<tipo>/<nome>`.
- Pacotes Java em minúsculas, base `com.cashflow.analytics` (use `cf-mkpkg`).
- .NET: `DbSet` no plural, `Id` (não `ID`), namespace = caminho da pasta ([`docs/backend/dotnet-core-banking.md`](../../docs/backend/dotnet-core-banking.md)).
- Nunca `dotnet ef ... --force` nem migrações EF Core sem confirmar.

## Mantendo os dois em sincronia
Ao criar uma função em um arquivo, crie a equivalente no outro **com o mesmo nome** e atualize a tabela de comandos acima.

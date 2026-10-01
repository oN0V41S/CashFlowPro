#!/usr/bin/env node
// Context guard: warns Claude when the context window is filling up, so it can
// suggest /compact before the auto-compact fires mid-turn.
//
// Wired to UserPromptSubmit and PreToolUse. Reads the exact token usage that the
// API reported on the last main-thread assistant message in the transcript
// (input + cache read + cache creation), so no estimation is needed.
//
// Env overrides:
//   CF_CONTEXT_WINDOW  context window size in tokens (default 200000; use 1000000 for 1M models)
//   CF_CONTEXT_WARN    warning threshold in percent (default 70)
//   CF_CONTEXT_STEP    re-warn only after usage grows by this many percent points (default 5)

import { closeSync, existsSync, fstatSync, openSync, readFileSync, readSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WINDOW = Number(process.env.CF_CONTEXT_WINDOW) || 200_000;
const WARN_PCT = Number(process.env.CF_CONTEXT_WARN) || 70;
const STEP_PCT = Number(process.env.CF_CONTEXT_STEP) || 5;
const TAIL_BYTES = 512 * 1024;

function readStdin() {
  try {
    return JSON.parse(readFileSync(0, "utf8"));
  } catch {
    return null;
  }
}

function readTail(path) {
  const fd = openSync(path, "r");
  try {
    const size = fstatSync(fd).size;
    const length = Math.min(size, TAIL_BYTES);
    const buffer = Buffer.alloc(length);
    readSync(fd, buffer, 0, length, size - length);
    return buffer.toString("utf8");
  } finally {
    closeSync(fd);
  }
}

function lastContextTokens(transcriptPath) {
  const lines = readTail(transcriptPath).split("\n");
  for (let i = lines.length - 1; i >= 0; i--) {
    let entry;
    try {
      entry = JSON.parse(lines[i]);
    } catch {
      continue; // first line of the tail may be cut in half
    }
    const usage = entry?.message?.usage;
    if (entry?.type !== "assistant" || entry.isSidechain || !usage) continue;
    return (
      (usage.input_tokens ?? 0) +
      (usage.cache_read_input_tokens ?? 0) +
      (usage.cache_creation_input_tokens ?? 0)
    );
  }
  return null;
}

function shouldWarn(sessionId, pct) {
  const statePath = join(tmpdir(), `cf-context-guard-${sessionId}.json`);
  let lastWarned = 0;
  if (existsSync(statePath)) {
    try {
      lastWarned = JSON.parse(readFileSync(statePath, "utf8")).lastWarned ?? 0;
    } catch {
      /* corrupt state: warn again */
    }
  }
  // Usage dropping means a compaction happened: reset so the next climb warns again.
  if (pct < lastWarned - STEP_PCT) lastWarned = 0;
  if (lastWarned > 0 && pct < lastWarned + STEP_PCT) return false;
  writeFileSync(statePath, JSON.stringify({ lastWarned: pct }));
  return true;
}

const input = readStdin();
if (!input?.transcript_path || !existsSync(input.transcript_path)) process.exit(0);

const tokens = lastContextTokens(input.transcript_path);
if (tokens === null) process.exit(0);

const pct = Math.round((tokens / WINDOW) * 100);
if (pct < WARN_PCT || !shouldWarn(input.session_id ?? "unknown", pct)) process.exit(0);

const message =
  `[context-guard] Context is at ~${pct}% (${tokens.toLocaleString("en-US")} / ${WINDOW.toLocaleString("en-US")} tokens). ` +
  `Auto-compact fires near 83% and can lose details mid-task. ` +
  `Finish only the current atomic step, then tell the user to run /compact before starting anything large. ` +
  `Delegate heavy file exploration to subagents to keep the main context small.`;

process.stdout.write(
  JSON.stringify({
    hookSpecificOutput: {
      hookEventName: input.hook_event_name,
      additionalContext: message,
    },
  }),
);

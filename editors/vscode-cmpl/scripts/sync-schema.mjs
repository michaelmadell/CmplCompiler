// Copies the repository's canonical cmpl.schema.json into the extension so
// the packaged .vsix is self-contained (vsce only packages files inside the
// extension folder). Run via `npm run sync-schema`; also wired into
// `vscode:prepublish`. CI fails if the committed copy drifts from the source.
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const source = resolve(here, "../../../cmpl.schema.json");
const destDir = resolve(here, "../schemas");
const dest = resolve(destDir, "cmpl.schema.json");

mkdirSync(destDir, { recursive: true });

// Re-serialize through JSON so the copy is normalized (stable formatting,
// trailing newline) regardless of the source file's whitespace.
const schema = JSON.parse(readFileSync(source, "utf8"));
writeFileSync(dest, JSON.stringify(schema, null, 2) + "\n");

console.log(`Synced schema: ${source} -> ${dest}`);

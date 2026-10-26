# LoomEditor.Lsp.FakeServer

A deterministic language server (test double) that speaks the real LSP over stdio. Every automated LSP test uses it, so CI
never needs a real language server. It keeps **one** open document. Standard output carries protocol frames only; human
diagnostics go to standard error.

Run it: `LoomEditor.Lsp.FakeServer [--sync=none|full|incremental] [--delay=<ms>] [--exit-on=<method>] [--stderr=<text>] [--incomplete] [--chatty]`.
`--selftest` checks the protocol types and prints `ok`. In process: `new FakeLanguageServer(input, output, options).RunAsync(token)`.

## Lifecycle

- Before `initialize` a request gets `-32002 Server not initialized`; notifications are ignored (except `exit`).
- `initialize` answers: `positionEncoding utf-16`, `textDocumentSync { openClose: true, change: 0|1|2 }` (`--sync`),
  completion (trigger character `.`, resolve), hover, definition, `serverInfo LoomEditor.FakeServer 1.0`. A second `initialize` is `-32600`.
- `shutdown` answers `null`; later requests get `-32600 Server is shutting down`. An unknown request is `-32601`, an unknown notification is ignored.
- Exit code: `exit` after `shutdown` 0; `exit` alone 1; input ends after `shutdown` 0, otherwise 1; `--exit-on=<method>` 3.

## Document and diagnostics

`didOpen` stores uri, version and text; `didChange` applies every change in order (no range: replace all; with range: UTF-16
positions, a character beyond the line is clamped to the line end, a line beyond the document to the end of the document; lines end with `\n`, `\r\n` or `\r`).
A change for another uri or with a version `<=` the current one is ignored and recorded as `didChange(ignored)`.
`didClose` clears the document and publishes an empty list. Diagnostics are published after every open, accepted change and close
(`{ uri, version, diagnostics }`, source `fake`, message `Found <WORD>`) for the whole words:
`ERROR` severity 1 code `"FAKE001"`, `WARN` 2 code `2`, `INFO` 3, `HINT` 4, `UNUSED` 4 tag 1, `OLD` 4 tag 2.

## Features

- **Completion**: `{ isIncomplete: --incomplete, items }`. With trigger character `.` the member items (`Length`, `Count` kind 10; `ToString`, `Equals` kind 2 with
  commit character `(`; each with an insert/replace `textEdit`) come first, then, always, the distinct words of the document of two characters or more
  (ordinal order, kind 1, `insertText`, without the occurrence that ends at the cursor) and the keywords `if` (preselected), `else`, `for`, `class`. Every item has `sortText = label` and `data { label, n: 1 }`.
- **Resolve**: adds `detail` and markdown `documentation`; `Length` also gets an additional edit; `data` unchanged.
- **Hover**: the identifier at the position (its end counts), markdown contents, its range; none: `null`.
- **Definition**: the word `external` gives a `Location` in `file:///other/file.txt`; `def <word>` a `Location`; `class <word>` a `LocationLink[]`; otherwise `null`.

## Misbehavior

`--delay=<ms>` delays completion, resolve, hover and definition answers (the read loop keeps reading); `$/cancelRequest` of a waiting request answers `-32800` at once.
`--chatty` sends, after `initialized`: `window/logMessage`, requests `cfg-1` (`workspace/configuration`), `reg-1` (`client/registerCapability`), `wdp-1`
(`window/workDoneProgress/create`), `edit-1` (`workspace/applyEdit`) and `window/showMessage`; the responses are recorded as `response(<id>)`.

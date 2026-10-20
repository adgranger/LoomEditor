// Regenerates the golden token files of tests/LoomEditor.TextMate.Tests/Golden with the REAL vscode-textmate and
// vscode-oniguruma, from LoomEditor's own grammar and theme files. Run from eng/golden: `npm ci && node generate.mjs`.
// It is never run by the build or by the tests. Needs Node 20+.
//
// The theme pipeline is VS Code's (colorThemeData.ts): includes, rule 0, normalizeColor, TokenColorIndex.
// Token capture is VS Code's (themes.test.contribution.ts): tokenizeLine for scopes, tokenizeLine2 + color map for colors.

import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const tm = require('vscode-textmate');
const onig = require('vscode-oniguruma');

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..', '..');
const grammarsDir = path.join(root, 'src', 'LoomEditor.TextMate', 'Grammars');
const themesDir = path.join(root, 'src', 'LoomEditor.Themes', 'BuiltIn');
const goldenDir = path.join(root, 'tests', 'LoomEditor.TextMate.Tests', 'Golden');

const THEMES = [
    { key: 'darkModern', file: 'dark_modern.json', uiTheme: 'vs-dark' },
    { key: 'lightModern', file: 'light_modern.json', uiTheme: 'vs' },
];

// ---- JSON with comments and trailing commas (what VS Code theme files use)

function parseJsonc(text) {
    let out = '';
    let i = 0;
    while (i < text.length) {
        const c = text[i];
        if (c === '"') {
            let j = i + 1;
            while (text[j] !== '"') {
                j += text[j] === '\\' ? 2 : 1;
            }
            out += text.slice(i, j + 1);
            i = j + 1;
        } else if (c === '/' && text[i + 1] === '/') {
            while (i < text.length && text[i] !== '\n') {
                i++;
            }
        } else if (c === '/' && text[i + 1] === '*') {
            i = text.indexOf('*/', i + 2) + 2;
        } else {
            out += c;
            i++;
        }
    }
    return JSON.parse(out.replace(/,(\s*[}\]])/g, '$1'));
}

// ---- Languages and grammars (the same fields as language.json, see LanguageDefinitionReader.cs)

function readLanguages() {
    const languages = []; // { id, folder, grammar, extras }
    for (const folder of fs.readdirSync(grammarsDir, { withFileTypes: true }).filter((d) => d.isDirectory()).map((d) => d.name).sort()) {
        for (const file of fs.readdirSync(path.join(grammarsDir, folder)).filter((f) => f.endsWith('language.json')).sort()) {
            const json = parseJsonc(fs.readFileSync(path.join(grammarsDir, folder, file), 'utf8'));
            languages.push({ id: json.id, folder, json });
        }
    }
    return languages;
}

function grammarOf(node, folder) {
    if (typeof node.scopeName !== 'string' || typeof node.path !== 'string') {
        return null;
    }
    return {
        location: path.join(grammarsDir, folder, node.path),
        scopeName: node.scopeName,
        embeddedLanguages: node.embeddedLanguages ?? {},
        tokenTypes: node.tokenTypes ?? {},
        injectTo: node.injectTo,
        balancedBracketScopes: node.balancedBracketScopes ?? ['*'],
        unbalancedBracketScopes: node.unbalancedBracketScopes ?? [],
    };
}

const languages = readLanguages();
// plaintext is language 1; the others follow in registration order (the ids only need to be distinct).
const languageIds = new Map([['plaintext', 1]]);
for (const language of languages) {
    languageIds.set(language.id, languageIds.size + 1);
}

const grammarsByScope = new Map();
const injections = new Map(); // scope to inject into -> injecting scopes
const languageToScope = new Map();
for (const language of languages) {
    const all = [grammarOf(language.json, language.folder), ...(language.json.grammars ?? []).map((g) => grammarOf(g, language.folder))].filter(Boolean);
    for (const grammar of all) {
        grammarsByScope.set(grammar.scopeName, grammar);
        for (const target of grammar.injectTo ?? []) {
            injections.set(target, [...(injections.get(target) ?? []), grammar.scopeName]);
        }
    }
    const main = grammarOf(language.json, language.folder);
    if (main) {
        languageToScope.set(language.id, main.scopeName);
    }
}

// vscode-textmate StandardTokenType (not exported at runtime): Other = 0, Comment = 1, String = 2, RegEx = 3.
const TOKEN_TYPES = { other: 0, comment: 1, string: 2, regex: 3 };

function configurationOf(grammar) {
    const embeddedLanguages = {};
    for (const [scope, language] of Object.entries(grammar.embeddedLanguages)) {
        if (languageIds.has(language)) {
            embeddedLanguages[scope] = languageIds.get(language);
        }
    }
    const tokenTypes = {};
    for (const [scope, type] of Object.entries(grammar.tokenTypes)) {
        if (type in TOKEN_TYPES) {
            tokenTypes[scope] = TOKEN_TYPES[type];
        }
    }
    return {
        embeddedLanguages,
        tokenTypes,
        balancedBracketSelectors: grammar.balancedBracketScopes,
        unbalancedBracketSelectors: grammar.unbalancedBracketScopes,
    };
}

// ---- Themes (colorThemeData.ts)

// normalizeColor: #RGB, #RGBA, #RRGGBB or #RRGGBBAA as upper case #RRGGBB / #RRGGBBAA (an FF alpha is dropped).
function normalizeColor(color) {
    if (!color || typeof color !== 'string') {
        return undefined;
    }
    if (!/^#([0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$/i.test(color)) {
        return undefined;
    }
    let hex = color.slice(1).toUpperCase();
    if (hex.length <= 4) {
        hex = [...hex].map((c) => c + c).join('');
    }
    return '#' + (hex.length === 8 && hex.endsWith('FF') ? hex.slice(0, 6) : hex);
}

const DEFAULT_THEME_COLORS = {
    'vs': [['token.info-token', '#316bcd'], ['token.warn-token', '#cd9731'], ['token.error-token', '#cd3131'], ['token.debug-token', '#800080']],
    'vs-dark': [['token.info-token', '#6796e6'], ['token.warn-token', '#cd9731'], ['token.error-token', '#f44747'], ['token.debug-token', '#b267e6']],
};

function loadThemeFile(file, colors, rules) {
    const theme = parseJsonc(fs.readFileSync(path.join(themesDir, file), 'utf8'));
    if (theme.include) {
        loadThemeFile(path.posix.normalize(path.posix.join(path.posix.dirname(file), theme.include)), colors, rules);
    }
    for (const [id, value] of Object.entries(theme.colors ?? {})) {
        if (value === 'default') {
            delete colors[id];
        } else {
            colors[id] = value;
        }
    }
    for (const rule of theme.tokenColors ?? []) {
        if (rule.scope && rule.settings) {
            rules.push(rule);
        }
    }
}

function loadTheme(definition) {
    const colors = {};
    const rules = [];
    loadThemeFile(definition.file, colors, rules);
    const foreground = colors['editor.foreground'];
    const background = colors['editor.background'];
    if (!foreground || !background) {
        throw new Error(`${definition.file}: editor.foreground and editor.background are needed`);
    }

    // The default rule (empty scope) is always first.
    const settings = [{ settings: { foreground: normalizeColor(foreground), background: normalizeColor(background) } }];
    let hasDefaultTokens = false;
    for (const rule of rules) {
        hasDefaultTokens ||= rule.scope === 'token.info-token';
        settings.push({
            scope: rule.scope,
            settings: {
                foreground: normalizeColor(rule.settings.foreground),
                background: normalizeColor(rule.settings.background),
                fontStyle: rule.settings.fontStyle,
            },
        });
    }
    if (!hasDefaultTokens) {
        for (const [scope, color] of DEFAULT_THEME_COLORS[definition.uiTheme]) {
            settings.push({ scope, settings: { foreground: normalizeColor(color) } });
        }
    }

    // TokenColorIndex: ids start at 1 (index 0 is unused), foreground then background of each rule.
    const colorMap = [undefined];
    const ids = new Map();
    for (const rule of settings) {
        for (const color of [rule.settings.foreground, rule.settings.background]) {
            if (color && !ids.has(color)) {
                ids.set(color, colorMap.length);
                colorMap.push(color);
            }
        }
    }
    return { rawTheme: { name: definition.key, settings }, colorMap };
}

// ---- Tokenization

const wasm = fs.readFileSync(require.resolve('vscode-oniguruma/release/onig.wasm')).buffer;
await onig.loadWASM(wasm);

const registry = new tm.Registry({
    onigLib: Promise.resolve({
        createOnigScanner: (sources) => new onig.OnigScanner(sources),
        createOnigString: (text) => new onig.OnigString(text),
    }),
    loadGrammar: async (scopeName) => {
        const grammar = grammarsByScope.get(scopeName);
        if (!grammar) {
            return null;
        }
        return tm.parseRawGrammar(fs.readFileSync(grammar.location, 'utf8'), grammar.location);
    },
    getInjections: (scopeName) => {
        const parts = scopeName.split('.');
        const result = [];
        for (let i = 1; i <= parts.length; i++) {
            result.push(...(injections.get(parts.slice(0, i).join('.')) ?? []));
        }
        return result;
    },
});

const FONT_STYLES = [[1, 'italic'], [2, 'bold'], [4, 'underline'], [8, 'strikethrough']];

function decodeFontStyle(metadata) {
    const bits = (metadata & 0x00007800) >>> 11;
    return FONT_STYLES.filter(([bit]) => bits & bit).map(([, name]) => name).join(' ');
}

function splitLines(text) {
    return text.split(/\r\n|\r|\n/);
}

const themes = THEMES.map((definition) => ({ definition, ...loadTheme(definition) }));
let failed = false;

for (const language of languages.filter((l) => fs.existsSync(path.join(goldenDir, l.id)))) {
    const dir = path.join(goldenDir, language.id);
    const sample = fs.readdirSync(dir).find((f) => f.startsWith('sample.') && !f.endsWith('.tokens.json'));
    if (!sample) {
        continue;
    }
    try {
        const main = grammarsByScope.get(languageToScope.get(language.id));
        const lines = splitLines(fs.readFileSync(path.join(dir, sample), 'utf8'));

        // Scopes do not depend on the theme.
        registry.setTheme(themes[0].rawTheme, themes[0].colorMap);
        let grammar = await registry.loadGrammarWithConfiguration(main.scopeName, languageIds.get(language.id), configurationOf(main));
        const scopeLines = [];
        let stack = tm.INITIAL;
        for (const text of lines) {
            const result = grammar.tokenizeLine(text, stack);
            scopeLines.push(result.tokens.map((t) => ({ start: t.startIndex, end: t.endIndex, scopes: t.scopes.join(' ') })));
            stack = result.ruleStack;
        }

        // Colors: a new pass per theme (a rule stack must not be used after setTheme).
        const colorLines = {};
        for (const theme of themes) {
            registry.setTheme(theme.rawTheme, theme.colorMap);
            grammar = await registry.loadGrammarWithConfiguration(main.scopeName, languageIds.get(language.id), configurationOf(main));
            const result = [];
            let stack2 = tm.INITIAL;
            for (const text of lines) {
                const tokens = grammar.tokenizeLine2(text, stack2);
                const list = [];
                for (let i = 0; i < tokens.tokens.length; i += 2) {
                    const metadata = tokens.tokens[i + 1];
                    list.push({
                        start: tokens.tokens[i],
                        foreground: theme.colorMap[(metadata & 0x00FF8000) >>> 15] ?? '',
                        fontStyle: decodeFontStyle(metadata),
                    });
                }
                result.push(list);
                stack2 = tokens.ruleStack;
            }
            colorLines[theme.definition.key] = result;
        }

        const out = [`{ "language": ${JSON.stringify(language.id)}, "lines": [`];
        lines.forEach((text, i) => {
            const entry = { text, tokens: scopeLines[i] };
            for (const theme of themes) {
                entry[theme.definition.key] = colorLines[theme.definition.key][i];
            }
            out.push(`  ${JSON.stringify(entry)}${i === lines.length - 1 ? '' : ','}`);
        });
        out.push('] }', '');
        fs.writeFileSync(path.join(dir, `${sample}.tokens.json`), out.join('\n'), 'utf8');
        console.log(`${language.id}: ${lines.length} lines`);
    } catch (e) {
        failed = true;
        console.error(`${language.id}: ${e.stack ?? e}`);
    }
}

process.exit(failed ? 1 : 0);

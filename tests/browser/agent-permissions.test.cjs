const { test } = require('node:test');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const path = require('node:path');
const { loadRules, claudeSettings, codexRules, antigravityEntry } = require('../../tools/agent-permissions.cjs');

const generator = path.resolve(__dirname, '../../tools/agent-permissions.cjs');

test('generated agent permission files match tools/agent-permissions.json', () => {
    // Throws (exit 1) if .claude/settings.json or .codex/rules/project.rules was edited by hand or not regenerated.
    execFileSync(process.execPath, [generator, '--check'], { stdio: 'pipe' });
});

test('git push is refused and nothing destructive is pre-approved', () => {
    const entries = loadRules();
    const allowed = entries.filter((e) => e.decision === 'allow').map((e) => e.tokens.join(' '));
    assert.ok(entries.some((e) => e.decision === 'deny' && e.tokens.join(' ') === 'git push'));
    for (const risky of ['git push', 'git reset', 'git clean', 'docker rm', 'docker volume', 'rm', 'sqlcmd']) {
        assert.ok(!allowed.some((a) => a === risky || a.startsWith(risky + ' ')), `${risky} must not be allowed`);
    }
    assert.ok(!allowed.some((a) => a.includes('sqlcmd') || a.startsWith('docker exec')), 'raw SQL must ask');
});

test('every tool gets the same decision for each command', () => {
    const entries = loadRules();
    const claude = JSON.parse(claudeSettings(entries)).permissions;
    const codex = codexRules(entries);
    for (const e of entries) {
        const cmd = e.tokens.join(' ');
        assert.ok(claude[e.decision].includes(`Bash(${cmd}:*)`), `Claude ${e.decision}: ${cmd}`);
        const decision = { allow: 'allow', ask: 'prompt', deny: 'forbidden' }[e.decision];
        assert.ok(codex.includes(`match = [${JSON.stringify(cmd)}]`) && codex.includes(`"${decision}"`), cmd);
    }
    assert.equal(antigravityEntry({ tokens: ['tools/sql/cleanup-zz.sh'] }), 'command(tools/sql/cleanup-zz\\.sh)');
});

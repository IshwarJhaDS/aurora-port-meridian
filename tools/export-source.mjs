import { readFile, writeFile, readdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';

const files = ['package.json', 'index.html', 'style.css', 'lab.html', 'lab.css'];
async function sourceFiles(directory) {
  for (const entry of (await readdir(directory, { withFileTypes: true })).sort((a,b)=>a.name.localeCompare(b.name))) {
    const path = `${directory}/${entry.name}`;
    if(entry.isDirectory()&&!entry.name.startsWith('.')) await sourceFiles(path);
    else if(entry.isFile()&&/\.(js|mjs|py)$/.test(entry.name)) files.push(path);
  }
}
for(const directory of ['src','tests','tools'])await sourceFiles(directory);
const languages = { json: 'json', html: 'html', css: 'css', js: 'javascript', mjs: 'javascript', py: 'python' };
let text = '# Aurora Phase 2 — complete source listing\n\n';
text += 'This is the complete textual source, including shaders, runtime integration, tests and validation tools. '; 
text += 'Copy files using the exact paths shown, or use the supplied ZIP, which also contains all PNG texture assets. '; 
text += 'The architecture and mathematical derivations are in `PHASE2_ARCHITECTURE.md`.\n\n';
for (const file of files) {
  const contents = await readFile(file, 'utf8');
  text += `## ${file}\n\n\`\`\`${languages[file.split('.').at(-1)]}\n${contents.trimEnd()}\n\`\`\`\n\n`;
}
text += '## Included texture assets\n\n| File | Bytes | SHA-256 |\n| --- | ---: | --- |\n';
for (const file of (await readdir('assets')).sort()) {
  const contents = await readFile(`assets/${file}`);
  text += `| assets/${file} | ${contents.length} | ${createHash('sha256').update(contents).digest('hex')} |\n`;
}
await writeFile('docs/FULL_SOURCE.md', text);
console.log(`Exported ${files.length} complete source files.`);

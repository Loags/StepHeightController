const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');

const out = __dirname;
const stairLine = 'M185 620H342V510H500V400H658V290H815';

function artwork(width, height, kind) {
  const icon = kind === 'icon';
  const stairStroke = icon ? 22 : 12;
  const capsuleStroke = icon ? 20 : kind === 'card' ? 10 : 8;
  const bounds = { left: 185 - stairStroke / 2, right: 815 + stairStroke / 2, top: 65 - capsuleStroke / 2, bottom: 660 };
  const subjectHeight = height * (icon ? 0.76 : 0.78);
  const scale = subjectHeight / (bounds.bottom - bounds.top);
  const offsetX = width * 0.475 - (bounds.left + bounds.right) / 2 * scale;
  const offsetY = height / 2 - (bounds.top + bounds.bottom) / 2 * scale;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
  <defs>
    <radialGradient id="backdrop"><stop stop-color="#0b1025"/><stop offset="1" stop-color="#02040d"/></radialGradient>
    <linearGradient id="edge" x1="0" y1="1" x2="1" y2="0"><stop stop-color="#5730d8"/><stop offset=".48" stop-color="#527eff"/><stop offset="1" stop-color="#13def1"/></linearGradient>
    <linearGradient id="body" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#143747"/><stop offset=".55" stop-color="#0d1d35"/><stop offset="1" stop-color="#13132f"/></linearGradient>
    <linearGradient id="steps" x1="0" y1="1" x2="1" y2="0"><stop stop-color="#0a0a1f"/><stop offset="1" stop-color="#0c172a"/></linearGradient>
    <linearGradient id="capsuleEdge" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#a9f8ff"/><stop offset=".45" stop-color="#21d6ed"/><stop offset="1" stop-color="#6e5ce4"/></linearGradient>
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="14"/></filter>
  </defs>
  <rect width="${width}" height="${height}" fill="url(#backdrop)"/>
  <g transform="translate(${offsetX},${offsetY}) scale(${scale})">
    <path d="${stairLine}" fill="none" stroke="url(#edge)" stroke-width="24" opacity=".18" filter="url(#glow)"/>
    <rect x="690" y="65" width="100" height="215" rx="50" fill="none" stroke="#19d6f3" stroke-width="18" opacity=".13" filter="url(#glow)"/>
    <path d="M185 620H342V510H500V400H658V290H815V660H185Z" fill="url(#steps)"/>
    <path d="${stairLine}" fill="none" stroke="url(#edge)" stroke-width="${stairStroke}" stroke-linecap="round" stroke-linejoin="round"/>
    <rect x="690" y="65" width="100" height="215" rx="50" fill="url(#body)" stroke="url(#capsuleEdge)" stroke-width="${capsuleStroke}"/>
    <path d="M707 116C707 96 722 82 740 82" fill="none" stroke="#c1faff" stroke-opacity=".48" stroke-width="${icon ? 7 : 4}" stroke-linecap="round"/>
  </g>
</svg>`;
}

async function build() {
  const variants = [
    ['cover', 1950, 1300],
    ['card', 420, 280],
    ['icon', 160, 160],
    ['social', 1200, 630]
  ];
  for (const [name, width, height] of variants) {
    const source = artwork(width, height, name);
    fs.writeFileSync(path.join(out, `${name}.svg`), source);
    await sharp(Buffer.from(source)).png().toFile(path.join(out, `${name}-${width}x${height}.png`));
  }
  await sharp(path.join(out, 'icon-160x160.png')).resize(80, 80).png().toFile(path.join(out, 'review-icon-80x80.png'));
  await sharp(path.join(out, 'card-420x280.png')).resize(210, 140).png().toFile(path.join(out, 'review-card-210x140.png'));
  await sharp(path.join(out, 'cover-1950x1300.png')).resize(475, 325, { fit: 'cover', position: 'northwest' }).png().toFile(path.join(out, 'review-cover-475x325.png'));
  const socialSource = artwork(2400, 1260, 'social');
  fs.writeFileSync(path.join(out, 'social-crop-source.svg'), socialSource);
  await sharp(Buffer.from(socialSource)).png().toFile(path.join(out, 'social-crop-source-2400x1260.png'));
  console.log('Rendered individually framed text-free variants and observed portal-size review previews.');
}

build().catch(error => { console.error(error); process.exit(1); });

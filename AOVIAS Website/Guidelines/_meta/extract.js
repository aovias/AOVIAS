const { spawnSync } = require("child_process");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const fzstd = require("./node_modules/fzstd");
const Y = require("./node_modules/yjs");

const OUT_DIR = path.resolve(__dirname, "..");
const META_DIR = __dirname;
const RAW_DIR = path.join(META_DIR, "raw");
const EXPORTED_AT = new Date().toISOString().slice(0, 10);

const DOCS = [
  {
    view_id: "1a938c20-7929-4f7d-9bee-c1f04f90b1ad",
    title: "AOVIAS Autonomous Engineering Execution Framework (AEEF)",
    file: "01-aeef.md",
  },
  {
    view_id: "cb881e33-f6a2-4967-94bb-a8d19925f638",
    title: "AOVIAS Website Development Execution Rules",
    file: "02-development-execution-rules.md",
  },
  {
    view_id: "719c8699-bf80-49d5-a752-c4f6c2c85529",
    title: "AOVIAS Website Engineering & Coding Standards",
    file: "03-engineering-coding-standards.md",
  },
  {
    view_id: "4514dad4-ad61-4f8b-bb74-7f98d8a967f4",
    title: "AOVIAS Website Solution Architecture & Folder Structure",
    file: "04-solution-architecture-folder-structure.md",
  },
  {
    view_id: "e4a816af-9403-4d49-ba46-0733a019b870",
    title: "AOVIAS Website Component Architecture & Inventory",
    file: "05-component-architecture-inventory.md",
  },
  {
    view_id: "90e6abda-19f8-442f-a220-b6d01650c11c",
    title: "AOVIAS Enterprise Design Language",
    file: "06-enterprise-design-language.md",
  },
  {
    view_id: "b5269d6d-95d6-438b-8d33-88a2c4f5b750",
    title: "AOVIAS Motion Design System",
    file: "07-motion-design-system.md",
  },
  {
    view_id: "9df334fc-8066-4643-a423-8ff229d3d076",
    title: "AOVIAS Search, SEO & Discoverability Standards",
    file: "08-search-seo-discoverability-standards.md",
  },
  {
    view_id: "0e2beb4e-20d1-42a6-98d8-6fc7caaec2b5",
    title: "AOVIAS Website Definition of Done (DoD)",
    file: "09-definition-of-done.md",
  },
  {
    view_id: "1109b8f8-cb96-4ad1-9e19-b1f4cbc8f202",
    title:
      "AOVIAS Website Performance Engineering Standards & Performance Budget",
    file: "10-performance-engineering-standards.md",
  },
  {
    view_id: "946b660b-06e9-4651-b14e-5f04e111746c",
    title: "First Plan",
    file: "11-first-plan.md",
  },
];

const NESTED_DOD = "e1877af8-ea9d-45e7-a600-76cd2ab3c62e";

function run(cmd, args) {
  const r = spawnSync(cmd, args, {
    encoding: "utf8",
    maxBuffer: 50 * 1024 * 1024,
  });
  if (r.status !== 0) {
    throw new Error(
      `${cmd} failed: ${(r.stderr || r.stdout || "").toString().slice(0, 400)}`
    );
  }
  return r;
}

function fetchRestoreState(oid) {
  const remote = `/tmp/${oid}.hex`;
  const local = path.join(RAW_DIR, `${oid}.hex`);
  const sql = `COPY (SELECT encode(restore_state, 'hex') FROM af_collab_versions WHERE oid = '${oid}' ORDER BY created_at DESC LIMIT 1) TO STDOUT`;
  run("docker", [
    "exec",
    "appflowy-cloud-postgres-1",
    "sh",
    "-c",
    `psql -U postgres -t -A -c "${sql}" > ${remote}`,
  ]);
  run("docker", ["cp", `appflowy-cloud-postgres-1:${remote}`, local]);
  const hex = fs.readFileSync(local, "utf8").replace(/[^0-9a-fA-F]/g, "");
  if (!hex) throw new Error(`Empty restore_state for ${oid}`);
  return Buffer.from(hex, "hex");
}

function decompressCollab(buf) {
  if (buf[0] === 0x28 && buf[1] === 0xb5 && buf[2] === 0x2f && buf[3] === 0xfd) {
    return Buffer.from(fzstd.decompress(buf));
  }
  for (let i = 0; i < Math.min(buf.length, 64); i++) {
    if (buf[i] === 0x28 && buf[i + 1] === 0xb5 && buf[i + 2] === 0x2f && buf[i + 3] === 0xfd) {
      return Buffer.from(fzstd.decompress(buf.slice(i)));
    }
  }
  throw new Error("No zstd payload found");
}

function yTextToString(value) {
  if (value == null) return "";
  if (typeof value === "string") return value;
  if (value instanceof Y.Text) return value.toString();
  return String(value);
}

function parseBlockData(raw) {
  if (raw == null) return {};
  if (typeof raw === "object" && !(raw instanceof Y.Text)) return raw;
  const s = yTextToString(raw);
  if (!s) return {};
  try {
    return JSON.parse(s);
  } catch {
    return {};
  }
}

function getMapValue(map, key) {
  if (!map || typeof map.get !== "function") return undefined;
  return map.get(key);
}

function blockToMarkdown(ty, text, data) {
  const t = (text || "").trim();
  switch (ty) {
    case "heading": {
      if (!t) return "";
      const level = Math.min(Math.max(Number(data.level) || 1, 1), 6);
      return `${"#".repeat(level + 1)} ${t}`; // +1 because doc title is H1
    }
    case "bulleted_list":
      return t ? `- ${t}` : "";
    case "numbered_list":
      return t ? `1. ${t}` : "";
    case "todo_list": {
      const checked = Boolean(data.checked);
      return t ? `- [${checked ? "x" : " "}] ${t}` : "";
    }
    case "quote":
      return t ? `> ${t}` : "";
    case "code":
      return t ? "```\n" + t + "\n```" : "";
    case "divider":
      return "---";
    case "paragraph":
    default:
      return t;
  }
}

function documentToMarkdown(payload) {
  const doc = new Y.Doc();
  Y.applyUpdate(doc, new Uint8Array(payload));

  const data = doc.getMap("data");
  const document = getMapValue(data, "document");
  if (!(document instanceof Y.Map)) {
    throw new Error("Missing data.document map");
  }

  const blocks = getMapValue(document, "blocks");
  const meta = getMapValue(document, "meta");
  const pageId = yTextToString(getMapValue(document, "page_id"));
  if (!(blocks instanceof Y.Map) || !(meta instanceof Y.Map)) {
    throw new Error("Missing blocks/meta");
  }

  const childrenMap = getMapValue(meta, "children_map");
  const textMap = getMapValue(meta, "text_map");
  if (!(childrenMap instanceof Y.Map) || !(textMap instanceof Y.Map)) {
    throw new Error("Missing children_map/text_map");
  }

  const lines = [];
  let blockCount = 0;
  let numberedCounter = 0;

  function walk(blockId, depth) {
    const block = getMapValue(blocks, blockId);
    if (!(block instanceof Y.Map)) return;

    const ty = yTextToString(getMapValue(block, "ty")) || "paragraph";
    const externalId =
      yTextToString(getMapValue(block, "external_id")) ||
      yTextToString(getMapValue(block, "id")) ||
      blockId;
    const dataField = parseBlockData(getMapValue(block, "data"));
    const text = yTextToString(getMapValue(textMap, externalId));

    if (ty !== "page") {
      if (ty === "numbered_list") numberedCounter += 1;
      else numberedCounter = 0;

      let md = blockToMarkdown(ty, text, dataField);
      if (ty === "numbered_list" && md) {
        md = md.replace(/^1\.\s/, `${numberedCounter}. `);
      }
      if (md) {
        // indent nested list items lightly
        if ((ty === "bulleted_list" || ty === "numbered_list" || ty === "todo_list") && depth > 1) {
          lines.push("  ".repeat(Math.min(depth - 1, 3)) + md);
        } else {
          lines.push(md);
        }
        lines.push("");
        blockCount++;
      } else if (ty === "divider") {
        lines.push("---");
        lines.push("");
        blockCount++;
      }
    }

    const childArr = getMapValue(childrenMap, blockId);
    if (childArr instanceof Y.Array) {
      for (let i = 0; i < childArr.length; i++) {
        walk(yTextToString(childArr.get(i)), depth + 1);
      }
    }
  }

  // Prefer page children order; fall back to pageId root
  const rootChildren = getMapValue(childrenMap, pageId);
  if (rootChildren instanceof Y.Array && rootChildren.length) {
    for (let i = 0; i < rootChildren.length; i++) {
      walk(yTextToString(rootChildren.get(i)), 1);
    }
  } else {
    walk(pageId, 0);
  }

  const body = lines.join("\n").replace(/\n{3,}/g, "\n\n").trim();
  return { body, blockCount };
}

function frontmatter(doc) {
  return [
    "---",
    `title: "${doc.title.replace(/"/g, '\\"')}"`,
    "source: appflowy",
    "workspace: AOVIAS Website",
    "space: General",
    `view_id: "${doc.view_id}"`,
    `exported_at: "${EXPORTED_AT}"`,
    "---",
    "",
    `# ${doc.title}`,
    "",
  ].join("\n");
}

function main() {
  fs.mkdirSync(OUT_DIR, { recursive: true });
  fs.mkdirSync(RAW_DIR, { recursive: true });

  const sourceMap = {
    workspace_id: "359ce6df-f951-4d69-b4e2-2ea0abc6e77a",
    workspace_name: "AOVIAS Website",
    space_id: "a3f32808-44d3-476e-aa50-ef0221557ea9",
    space_name: "General",
    exported_at: EXPORTED_AT,
    documents: [],
  };

  let canonicalDodHash = null;

  for (const doc of DOCS) {
    process.stdout.write(`Extracting ${doc.file} ... `);
    const raw = fetchRestoreState(doc.view_id);
    const payload = decompressCollab(raw);
    const { body, blockCount } = documentToMarkdown(payload);
    const content =
      frontmatter(doc) + (body || "_No extractable text found in collab state._") + "\n";
    fs.writeFileSync(path.join(OUT_DIR, doc.file), content, "utf8");

    const hash = crypto.createHash("sha256").update(raw).digest("hex");
    if (doc.view_id === "0e2beb4e-20d1-42a6-98d8-6fc7caaec2b5") canonicalDodHash = hash;

    sourceMap.documents.push({
      title: doc.title,
      view_id: doc.view_id,
      file: doc.file,
      bytes: raw.length,
      extract_method: "zstd+yjs-blocks",
      blocks: blockCount,
      body_chars: body.length,
      sha256: hash,
    });
    console.log(`blocks=${blockCount} chars=${body.length}`);
  }

  let nestedDodHash = null;
  try {
    nestedDodHash = crypto
      .createHash("sha256")
      .update(fetchRestoreState(NESTED_DOD))
      .digest("hex");
  } catch (e) {
    nestedDodHash = `error:${e.message}`;
  }

  sourceMap.dod_dedup = {
    canonical_view_id: "0e2beb4e-20d1-42a6-98d8-6fc7caaec2b5",
    nested_view_id: NESTED_DOD,
    identical_restore_state: canonicalDodHash === nestedDodHash,
    note:
      canonicalDodHash === nestedDodHash
        ? "Nested Getting started DoD matches General DoD; only canonical file written."
        : "Nested Getting started DoD differs from General copy; canonical General copy kept.",
  };

  fs.writeFileSync(
    path.join(META_DIR, "source-map.json"),
    JSON.stringify(sourceMap, null, 2) + "\n",
    "utf8"
  );

  const readme = `# AOVIAS Website Guidelines

Local Markdown copy of **AOVIAS Website → General** documents from self-hosted AppFlowy Cloud.

Use these as the development guidelines for the AOVIAS Website project.

## Source

| Field | Value |
| --- | --- |
| Workspace | AOVIAS Website |
| Workspace ID | \`359ce6df-f951-4d69-b4e2-2ea0abc6e77a\` |
| Space | General |
| Exported | ${EXPORTED_AT} |
| Method | AppFlowy Postgres \`af_collab_versions.restore_state\` → zstd → Yjs document blocks |

## Documents

${DOCS.map((d, i) => `${i + 1}. [${d.title}](./${d.file})`).join("\n")}

## Exclusions

- To-dos board
- Untitled page
- Stock AppFlowy starter pages (Getting started, Desktop/Mobile/Web guide)
- Nested duplicate DoD under Getting started — ${sourceMap.dod_dedup.note}

## Provenance

See [\`_meta/source-map.json\`](./_meta/source-map.json) for \`view_id\` → file mapping and extraction metadata.

## How to use

1. Treat these Markdown files as the working guidelines for website implementation.
2. Prefer this folder over re-opening AppFlowy for day-to-day coding reference.
3. Re-run \`node _meta/extract.js\` (with AppFlowy Docker up) if documents change and you need a fresh sync.
`;

  fs.writeFileSync(path.join(OUT_DIR, "README.md"), readme, "utf8");
  console.log("Wrote README.md and source-map.json");
}

main();

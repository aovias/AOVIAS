import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = path.resolve("outputs", "praman_success_checklist");
await fs.mkdir(outputDir, { recursive: true });

const sections = [
  {
    no: 1,
    name: "Compliance Framework Engine",
    groups: {
      "Framework Management": [
        "Frameworks (NABH Entry, NABH Full, NABL, JCI)",
        "Chapters",
        "Standards",
        "Objective Elements",
        "Versioning",
        "Effective dates",
        "Framework comparison",
      ],
      Classification: ["Core", "Commitment", "Achievement", "Excellence"],
      Applicability: ["Department applicability", "Hospital type applicability", "Service applicability"],
    },
  },
  {
    no: 2,
    name: "Organization Structure",
    groups: {
      "Hospital Setup": ["Hospital profile", "Branches", "Locations", "Departments", "Units", "Services"],
      "User Management": [
        "Role-based access",
        "Department ownership",
        "Compliance officer",
        "Department coordinator",
        "Auditor",
        "Consultant",
        "Management",
      ],
    },
  },
  {
    no: 3,
    name: "Compliance Journey Engine",
    groups: {
      Journey: [
        "Enrollment",
        "Baseline assessment",
        "Gap assessment",
        "Action planning",
        "Evidence collection",
        "Internal audit",
        "Mock audit",
        "Accreditation readiness",
        "Surveillance",
        "Re-accreditation",
      ],
      "Operational Modes": [
        "Self-assessment",
        "Internal audit",
        "External audit preparation",
        "Surveillance",
        "Reaccreditation",
      ],
    },
  },
  {
    no: 4,
    name: "Obligation Engine",
    groups: {
      "Compliance Obligations": [
        "Obligation creation",
        "Due dates",
        "Recurring obligations",
        "Ownership",
        "Escalations",
        "Compliance status",
      ],
      Examples: [
        "Monthly fire drill",
        "Hand hygiene audit",
        "Equipment calibration",
        "Policy review",
        "Training renewal",
      ],
    },
  },
  {
    no: 5,
    name: "Evidence Management",
    groups: {
      "Evidence Binder": [
        "Upload evidence",
        "Versioning",
        "Approval workflow",
        "Expiry tracking",
        "Validity tracking",
        "Evidence tagging",
        "Evidence mapping to OEs",
      ],
      "Evidence Health": [
        "Missing evidence",
        "Expired evidence",
        "Soon-to-expire evidence",
        "Duplicate evidence",
      ],
    },
  },
  {
    no: 6,
    name: "Deficiency Engine",
    groups: {
      "Gap Identification": [
        "Auto-generated deficiencies",
        "Manual deficiencies",
        "Audit deficiencies",
        "Evidence deficiencies",
      ],
      Severity: ["Critical", "Major", "Minor", "Observation"],
      Lifecycle: ["Open", "Assigned", "CAPA initiated", "Under review", "Closed"],
    },
  },
  {
    no: 7,
    name: "CAPA Management",
    groups: {
      "Corrective Actions": [
        "Root cause analysis",
        "Action planning",
        "Owner assignment",
        "Due dates",
        "Verification",
      ],
      "Preventive Actions": ["Recurrence prevention", "Effectiveness review"],
    },
  },
  {
    no: 8,
    name: "Audit Management",
    groups: {
      "Audit Planning": ["Audit calendar", "Audit schedules", "Audit teams", "Checklists"],
      "Audit Execution": ["Mobile audit", "Findings", "Photos", "Notes", "Evidence linking"],
      "Audit Reporting": ["Findings summary", "Department score", "Readiness score"],
    },
  },
  {
    no: 9,
    name: "Compliance Scoring Engine",
    groups: {
      Scores: ["Chapter score", "Standard score", "Department score", "Hospital score", "Trend score"],
      Readiness: ["Accreditation readiness %", "Risk score", "Compliance maturity score"],
    },
  },
  {
    no: 10,
    name: "Policy & Document Management",
    groups: {
      Policies: ["Policy repository", "Version control", "Review workflow", "Approval workflow"],
      Documents: ["SOPs", "Forms", "Checklists", "Manuals"],
    },
  },
  {
    no: 11,
    name: "Training & Competency",
    groups: {
      "Staff Training": ["Training matrix", "Mandatory training", "Certification tracking", "Attendance"],
      Competency: ["Skill assessments", "Competency renewals"],
    },
  },
  {
    no: 12,
    name: "Incident & Risk Management",
    groups: {
      Incidents: ["Incident reporting", "Near miss reporting", "Sentinel events", "Investigation"],
      Risk: ["Risk register", "Risk scoring", "Mitigation plans"],
    },
  },
  {
    no: 13,
    name: "Dashboard & Analytics",
    groups: {
      "Command Center": [
        "Compliance score",
        "Open deficiencies",
        "CAPA status",
        "Audit readiness",
        "Department performance",
      ],
      "Executive View": ["Accreditation readiness", "Top risks", "Overdue actions", "Compliance trends"],
    },
  },
  {
    no: 14,
    name: "Notifications & Escalations",
    groups: {
      Alerts: ["Upcoming due dates", "Overdue actions", "Missing evidence", "Audit reminders"],
      Escalation: ["Department head", "Compliance officer", "Hospital management"],
    },
  },
  {
    no: 15,
    name: "Consultant Workspace",
    groups: {
      "Consultant Portal": [
        "Multi-hospital management",
        "Readiness view",
        "Audit planning",
        "Consultant notes",
        "Progress tracking",
      ],
    },
  },
  {
    no: 16,
    name: "Reporting",
    groups: {
      "Accreditation Reports": ["Chapter-wise reports", "Deficiency reports", "CAPA reports", "Audit reports"],
      Export: ["PDF", "Excel", "Evidence package"],
    },
  },
  {
    no: 17,
    name: "Enterprise & Security",
    groups: {
      Security: ["Audit logs", "Activity history", "MFA", "SSO", "Encryption"],
      "Multi-Tenant": ["Tenant isolation", "Subscription plans", "Usage tracking"],
    },
  },
  {
    no: 18,
    name: "AI Features (After Core is Stable)",
    groups: {
      "AI Compliance Assistant": [
        "Explain NABH standards",
        "Recommend evidence",
        "Identify gaps",
        "Generate CAPA suggestions",
        "Audit preparation assistance",
      ],
      "AI Insights": ["Predict accreditation risks", "Predict delayed CAPAs", "Compliance trend analysis"],
    },
  },
  {
    no: 19,
    name: "Non-Negotiable Success Criteria",
    groups: {
      "Instant Answers": [
        "Are we accreditation-ready?",
        "What standards are failing?",
        "Which departments are lagging?",
        "What evidence is missing?",
        "What evidence has expired?",
        "What CAPAs are overdue?",
        "What audits are pending?",
        "What obligations are due this month?",
        "What risks threaten accreditation?",
        "What should we do next?",
      ],
    },
  },
];

const rows = [];
for (const section of sections) {
  const phase =
    section.no <= 10 ? "Pilot Core" : section.no === 19 ? "Value Proof" : "Growth Layer";
  for (const [group, items] of Object.entries(section.groups)) {
    for (const item of items) {
      rows.push(["☐", null, section.no, section.name, group, item, phase, "Not Started", "", "", ""]);
    }
  }
}

const workbook = Workbook.create();
const dashboard = workbook.worksheets.add("Dashboard");
const checklist = workbook.worksheets.add("Checklist");
const lookups = workbook.worksheets.add("Lookups");

for (const sheet of [dashboard, checklist, lookups]) {
  sheet.showGridLines = false;
}

lookups.getRange("A1:D1").values = [["Checkbox", "Status", "Phase", "Owner"]];
lookups.getRange("A2:A3").values = [["☐"], ["☑"]];
lookups.getRange("B2:B6").values = [["Not Started"], ["In Progress"], ["Blocked"], ["Done"], ["Not Applicable"]];
lookups.getRange("C2:C4").values = [["Pilot Core"], ["Growth Layer"], ["Value Proof"]];
lookups.getRange("D2:D8").values = [
  ["Compliance Officer"],
  ["Department Coordinator"],
  ["Department Head"],
  ["Auditor"],
  ["Consultant"],
  ["Management"],
  ["Product Owner"],
];
lookups.getRange("A1:D1").format = { fill: "#0F766E", font: { bold: true, color: "#FFFFFF" } };
lookups.getRange("A1:D8").format.borders = { preset: "all", style: "thin", color: "#D9E2E0" };

checklist.getRange("A1:K1").merge();
checklist.getRange("A1").values = [["PRAMAN Success Checklist"]];
checklist.getRange("A1").format = {
  fill: "#0F766E",
  font: { bold: true, color: "#FFFFFF", size: 18 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
checklist.getRange("A2:K2").merge();
checklist.getRange("A2").values = [[
  "Use the checkbox dropdown in column A to mark completion. Sections 1-10 are the pilot core; 11-18 extend depth and enterprise value.",
]];
checklist.getRange("A2").format = {
  fill: "#E6F4F1",
  font: { color: "#134E4A", italic: true },
  wrapText: true,
};

const headers = [
  "Done",
  "Complete Flag",
  "Section No.",
  "Section",
  "Subsection",
  "Checklist Item",
  "Phase",
  "Status",
  "Owner",
  "Target Date",
  "Notes",
];
checklist.getRange("A3:K3").values = [headers];
checklist.getRange("A4:K" + (rows.length + 3)).values = rows;

const lastRow = rows.length + 3;
checklist.getRange(`B4:B${lastRow}`).formulas = rows.map((_, idx) => [`=IF(A${idx + 4}="☑",1,0)`]);
checklist.getRange(`A3:K${lastRow}`).format.borders = { preset: "all", style: "thin", color: "#DCE5E3" };
checklist.getRange("A3:K3").format = { fill: "#115E59", font: { bold: true, color: "#FFFFFF" } };
checklist.getRange(`A4:K${lastRow}`).format = { fill: "#FFFFFF", font: { color: "#1F2937" }, wrapText: true };
checklist.getRange(`A4:A${lastRow}`).format = { horizontalAlignment: "center", font: { size: 14 } };
checklist.getRange(`B4:B${lastRow}`).format = { font: { color: "#FFFFFF" } };
checklist.getRange(`C4:C${lastRow}`).format = { horizontalAlignment: "center" };
checklist.getRange(`J4:J${lastRow}`).format.numberFormat = "yyyy-mm-dd";

checklist.getRange("A:A").format.columnWidthPx = 58;
checklist.getRange("B:B").format.columnWidthPx = 62;
checklist.getRange("C:C").format.columnWidthPx = 72;
checklist.getRange("D:D").format.columnWidthPx = 245;
checklist.getRange("E:E").format.columnWidthPx = 190;
checklist.getRange("F:F").format.columnWidthPx = 310;
checklist.getRange("G:G").format.columnWidthPx = 105;
checklist.getRange("H:H").format.columnWidthPx = 125;
checklist.getRange("I:I").format.columnWidthPx = 160;
checklist.getRange("J:J").format.columnWidthPx = 110;
checklist.getRange("K:K").format.columnWidthPx = 260;
checklist.getRange("A1:K1").format.rowHeightPx = 36;
checklist.getRange("A2:K2").format.rowHeightPx = 34;
checklist.freezePanes.freezeRows(3);

checklist.dataValidations.add({ range: `A4:A${lastRow}`, rule: { type: "list", formula1: "Lookups!$A$2:$A$3" } });
checklist.dataValidations.add({ range: `H4:H${lastRow}`, rule: { type: "list", formula1: "Lookups!$B$2:$B$6" } });
checklist.dataValidations.add({ range: `G4:G${lastRow}`, rule: { type: "list", formula1: "Lookups!$C$2:$C$4" } });
checklist.dataValidations.add({ range: `I4:I${lastRow}`, rule: { type: "list", formula1: "Lookups!$D$2:$D$8" } });

const table = checklist.tables.add(`A3:K${lastRow}`, true, "PRAMANChecklist");
table.style = "TableStyleMedium2";
table.showFilterButton = true;

dashboard.getRange("A1:H1").merge();
dashboard.getRange("A1").values = [["PRAMAN Success Checklist Dashboard"]];
dashboard.getRange("A1").format = {
  fill: "#0F766E",
  font: { bold: true, color: "#FFFFFF", size: 18 },
  horizontalAlignment: "center",
};
dashboard.getRange("A2:H2").merge();
dashboard.getRange("A2").values = [[
  "Live summary driven by the Checklist sheet. Update checkboxes, status, owner, and target dates there.",
]];
dashboard.getRange("A2").format = { fill: "#E6F4F1", font: { color: "#134E4A", italic: true } };

dashboard.getRange("A4:B8").values = [
  ["Metric", "Value"],
  ["Total checklist items", ""],
  ["Completed items", ""],
  ["Overall completion", ""],
  ["Pilot core completion (Sections 1-10)", ""],
];
dashboard.getRange("B5:B8").formulas = [
  [`=COUNTA(Checklist!F4:F${lastRow})`],
  [`=SUM(Checklist!B4:B${lastRow})`],
  ["=IF(B5=0,0,B6/B5)"],
  [`=IF(COUNTIF(Checklist!C4:C${lastRow},"<=10")=0,0,SUMIF(Checklist!C4:C${lastRow},"<=10",Checklist!B4:B${lastRow})/COUNTIF(Checklist!C4:C${lastRow},"<=10"))`],
];
dashboard.getRange("A4:B4").format = { fill: "#115E59", font: { bold: true, color: "#FFFFFF" } };
dashboard.getRange("A5:B8").format.borders = { preset: "all", style: "thin", color: "#DCE5E3" };
dashboard.getRange("B7:B8").format.numberFormat = "0%";

dashboard.getRange("D4:H4").merge();
dashboard.getRange("D4").values = [["Success Gate"]];
dashboard.getRange("D4").format = { fill: "#115E59", font: { bold: true, color: "#FFFFFF" } };
dashboard.getRange("D5:H8").values = [
  ["Pilot rule", "Confidently check 80% of Sections 1-10 for a realistic hospital pilot.", "", "", ""],
  ["Current core readiness", "", "", "", ""],
  ["Decision", "", "", "", ""],
  ["Next focus", "", "", "", ""],
];
dashboard.getRange("E6").formulas = [["=B8"]];
dashboard.getRange("E7").formulas = [["=IF(E6>=80%,\"Pilot-ready threshold met\",\"Below pilot-ready threshold\")"]];
dashboard.getRange("E8").formulas = [[`=IFERROR(INDEX(B12:B29,MATCH(MIN(E12:E29),E12:E29,0)),"")`]];
dashboard.getRange("E6").format.numberFormat = "0%";
dashboard.getRange("D5:H8").format.borders = { preset: "all", style: "thin", color: "#DCE5E3" };
dashboard.getRange("D5:H8").format.wrapText = true;

dashboard.getRange("A11:E11").values = [["Section", "Name", "Total", "Complete", "% Complete"]];
const summaryTextRows = sections.map((section) => [section.no, section.name]);
const summaryFormulaRows = sections.map((section, idx) => {
  const row = idx + 12;
  return [
    `=COUNTIF(Checklist!C$4:C$${lastRow},A${row})`,
    `=SUMIF(Checklist!C$4:C$${lastRow},A${row},Checklist!B$4:B$${lastRow})`,
    `=IF(C${row}=0,0,D${row}/C${row})`,
  ];
});
dashboard.getRange(`A12:B${sections.length + 11}`).values = summaryTextRows;
dashboard.getRange(`C12:E${sections.length + 11}`).formulas = summaryFormulaRows;
dashboard.getRange("A11:E11").format = { fill: "#115E59", font: { bold: true, color: "#FFFFFF" } };
dashboard.getRange(`A12:E${sections.length + 11}`).format.borders = { preset: "all", style: "thin", color: "#DCE5E3" };
dashboard.getRange(`E12:E${sections.length + 11}`).format.numberFormat = "0%";
dashboard.getRange("A:A").format.columnWidthPx = 80;
dashboard.getRange("B:B").format.columnWidthPx = 290;
dashboard.getRange("C:E").format.columnWidthPx = 105;
dashboard.getRange("D:H").format.columnWidthPx = 135;
dashboard.freezePanes.freezeRows(11);

dashboard.getRange("P11:Q11").values = [["Section", "% Complete"]];
dashboard.getRange(`P12:P${sections.length + 11}`).formulas = sections.map((_, idx) => {
  const row = idx + 12;
  return [`=A${row}&". "&LEFT(B${row},30)`];
});
dashboard.getRange(`Q12:Q${sections.length + 11}`).formulas = sections.map((_, idx) => {
  const row = idx + 12;
  return [`=E${row}`];
});
dashboard.getRange(`Q12:Q${sections.length + 11}`).format.numberFormat = "0%";
dashboard.getRange("P:Q").format.columnWidthPx = 1;

const chart = dashboard.charts.add("bar", dashboard.getRange(`P11:Q${sections.length + 11}`));
chart.title = "Completion by Section";
chart.hasLegend = false;
chart.yAxis = { numberFormatCode: "0%" };
chart.setPosition("G11", "N31");

const checklistPreview = await workbook.render({
  sheetName: "Checklist",
  range: "A1:K35",
  scale: 1,
  format: "png",
});
await fs.writeFile(path.join(outputDir, "checklist_preview.png"), new Uint8Array(await checklistPreview.arrayBuffer()));

const dashboardPreview = await workbook.render({
  sheetName: "Dashboard",
  range: "A1:N31",
  scale: 1,
  format: "png",
});
await fs.writeFile(path.join(outputDir, "dashboard_preview.png"), new Uint8Array(await dashboardPreview.arrayBuffer()));

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(path.join(outputDir, "PRAMAN_Success_Checklist.xlsx"));

const dashboardInspect = await workbook.inspect({
  kind: "table",
  range: "Dashboard!A1:H20",
  include: "values,formulas",
  tableMaxRows: 20,
  tableMaxCols: 8,
  maxChars: 6000,
});
console.log(dashboardInspect.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
  maxChars: 2000,
});
console.log(errors.ndjson);
console.log(path.join(outputDir, "PRAMAN_Success_Checklist.xlsx"));

import json
import re
import sys
from pathlib import Path
from pypdf import PdfReader

folder = Path(sys.argv[1])
payments = json.loads((folder / "payment-label-source.json").read_text(encoding="utf-8-sig"))
text = re.sub(r"\s+", " ", "\n".join(page.extract_text() or "" for page in PdfReader(folder / "G04-HD20250421.pdf").pages))
checks = []
for payment in payments:
    money = f"{int(payment['SoTien']):,}".replace(",", ".")
    expected = rf"{re.escape(payment['TenPT'])}\s*:\s*{re.escape(money)}\s*₫"
    checks.append({"method": payment["MaPT"], "catalog_name": payment["TenPT"],
                   "expected_money": money, "matches_pdf": bool(re.search(expected, text))})
result = {"checks": checks, "passed": all(item["matches_pdf"] for item in checks) and bool(checks),
          "scope": "Compare actual SQL method name and amount with PDF, preserving original hardcoded-accent check failure"}
(folder / "catalog-label-verdict.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(result, ensure_ascii=False))
